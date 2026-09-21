using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sparks.Api.Data;
using Sparks.Api.Dtos;
using Sparks.Api.Models;
using Sparks.Api.Services;

namespace Sparks.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly EmailService _emailService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(AppDbContext db, UserManager<ApplicationUser> userManager, EmailService emailService, ILogger<UsersController> logger)
    {
        _db = db;
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> List(
    [FromQuery] string? search,
    [FromQuery] Models.UserRole? role,
    [FromQuery] Models.UserStatus? status)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term) ||
                u.Email!.ToLower().Contains(term));
        }

        if (role is not null)
            query = query.Where(u => u.Role == role.Value);

        if (status is not null)
            query = query.Where(u => u.Status == status.Value);

        var users = await query.ToListAsync();

        var activeCounts = await _db.Tickets
            .Where(t => t.AssigneeId != null && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed)
            .GroupBy(t => t.AssigneeId!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        return users
            .Select(u => u.ToDto(activeCounts.GetValueOrDefault(u.Id, 0)))
            .OrderByDescending(u => u.CreatedAt)
            .ToList();
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserDto request)
    {
        var adminEmail = User.Identity?.Name ?? "Admin";

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            AvatarColor = AvatarColorFor(request.Role),
            MustChangePassword = true,
        };

        var tempPassword = GenerateTemporaryPassword();
        var result = await _userManager.CreateAsync(user, tempPassword);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Échec de la création de l'utilisateur {Email} par l'administrateur {AdminEmail}", request.Email, adminEmail);
            return BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });
        }

        await _userManager.AddToRoleAsync(user, request.Role.ToString());

        try
        {
            await _emailService.SendTemporaryPasswordEmailAsync(user.Email!, $"{user.FirstName} {user.LastName}", tempPassword);
            _logger.LogInformation("Création réussie de l'utilisateur {Email} (Rôle: {Role}) par l'administrateur {AdminEmail}", user.Email, user.Role, adminEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invitation email to {Email} for new user {UserId}. User was still created in the database.", user.Email, user.Id);
        }

        return user.ToDto(0);
    }



    private static string GenerateTemporaryPassword()
    {
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string digits = "23456789";
        const string special = "!@#$%^&*";
        const string all = lower + upper + digits + special;

        var chars = new char[12];
        chars[0] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        chars[1] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        chars[3] = special[RandomNumberGenerator.GetInt32(special.Length)];
        for (var i = 4; i < chars.Length; i++)
            chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];

        // shuffle so the guaranteed classes aren't always in the first 4 positions
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> Update(Guid id, UpdateUserDto request)
    {
        var adminEmail = User.Identity?.Name ?? "Admin";
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            _logger.LogWarning("Tentative de modification d'un utilisateur introuvable (ID: {UserId}) par l'administrateur {AdminEmail}", id, adminEmail);
            return NotFound(new { message = "User not found." });
        }

        var oldRole = user.Role;
        if (request.FirstName is not null) user.FirstName = request.FirstName;
        if (request.LastName is not null) user.LastName = request.LastName;
        if (request.Status is not null) user.Status = request.Status.Value;
        if (request.Availability is not null) user.Availability = request.Availability.Value;
        if (request.Role is not null && request.Role.Value != user.Role)
        {
            await _userManager.RemoveFromRoleAsync(user, user.Role.ToString());
            user.Role = request.Role.Value;
            await _userManager.AddToRoleAsync(user, user.Role.ToString());
            _logger.LogInformation("Changement de rôle pour l'utilisateur {Email} : {OldRole} -> {NewRole} par {AdminEmail}", user.Email, oldRole, user.Role, adminEmail);
        }

        await _userManager.UpdateAsync(user);
        _logger.LogInformation("Mise à jour des informations de l'utilisateur {Email} par l'administrateur {AdminEmail}", user.Email, adminEmail);

        var activeTickets = await _db.Tickets.CountAsync(t => t.AssigneeId == id && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed);
        return user.ToDto(activeTickets);
    }




    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var adminEmail = User.Identity?.Name ?? "Admin";
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            _logger.LogWarning("Tentative de suppression d'un utilisateur introuvable (ID: {UserId}) par {AdminEmail}", id, adminEmail);
            return NotFound(new { message = "User not found." });
        }

        if (user.Role == Models.UserRole.Admin)
        {
            var adminCount = await _db.Users.CountAsync(u => u.Role == Models.UserRole.Admin);
            if (adminCount <= 1)
            {
                _logger.LogWarning("Tentative bloquée : l'administrateur {AdminEmail} a essayé de supprimer le dernier Admin restant.", adminEmail);
                return BadRequest(new { message = "Cannot delete the last remaining Admin." });
            }
        }

        try
        {
            await _userManager.DeleteAsync(user);
            _logger.LogInformation("Suppression réussie de l'utilisateur {Email} par l'administrateur {AdminEmail}", user.Email, adminEmail);
        }
        catch (DbUpdateException)
        {
            _logger.LogWarning("Échec de la suppression de l'utilisateur {Email} (contraintes de clés étrangères) par {AdminEmail}", user.Email, adminEmail);
            return Conflict(new { message = "This user has ticket history, comments or notifications and cannot be permanently deleted. Deactivate the account instead." });
        }
        return NoContent();
    }




    [HttpPost("{id}/reset-password")]
    public async Task<ActionResult> ResetPassword(Guid id, ResetPasswordDto request)
    {
        var adminEmail = User.Identity?.Name ?? "Admin";
        if (request.NewPassword != request.ConfirmPassword)
        {
            _logger.LogWarning("Échec de réinitialisation de mot de passe par l'admin {AdminEmail} : les mots de passe ne correspondent pas.", adminEmail);
            return BadRequest(new { message = "Passwords do not match." });
        }

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            _logger.LogWarning("Tentative de réinitialisation de mot de passe pour un utilisateur introuvable (ID: {UserId}) par {AdminEmail}", id, adminEmail);
            return NotFound(new { message = "User not found." });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Échec de réinitialisation du mot de passe pour l'utilisateur {Email} par {AdminEmail}", user.Email, adminEmail);
            return BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });
        }

        _logger.LogInformation("Réinitialisation réussie du mot de passe pour l'utilisateur {Email} par l'administrateur {AdminEmail}", user.Email, adminEmail);
        return Ok(new { message = "Password reset successfully." });
    }

    private static string AvatarColorFor(Models.UserRole role) => role switch
    {
        Models.UserRole.Admin => "#7C3AED",
        Models.UserRole.Specialist => "#043962",
        Models.UserRole.TeamLead => "#B45309",
        Models.UserRole.Polyvalent => "#0F766E",
        _ => "#008BD2",
    };
}
