using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sparks.Api.Data;
using Sparks.Api.Dtos;
using Sparks.Api.Models;
using Sparks.Api.Services;

using Microsoft.AspNetCore.RateLimiting;

namespace Sparks.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly TokenService _tokenService;
    private readonly AppDbContext _db;
    private readonly EmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        TokenService tokenService,
        AppDbContext db,
        EmailService emailService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _db = db;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth-policy")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || user.Status == UserStatus.Inactive)
        {
            _logger.LogWarning("Tentative de connexion échouée : utilisateur introuvable ou inactif ({Email})", request.Email);
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            _logger.LogWarning("Compte verrouillé suite à de multiples échecs pour l'utilisateur : {Email}", request.Email);
            return Unauthorized(new { message = "Account locked due to too many failed attempts. Please try again in 15 minutes." });
        }
        if (!result.Succeeded)
        {
            _logger.LogWarning("Tentative de connexion échouée (mot de passe incorrect) pour l'utilisateur : {Email}", request.Email);
            return Unauthorized(new { message = "Invalid email or password." });
        }

        _logger.LogInformation("Connexion réussie pour l'utilisateur : {Email} (Rôle: {Role})", user.Email, user.Role);
        return await IssueTokens(user);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponseDto>> Refresh(RefreshRequestDto request)
    {
        var existing = _db.RefreshTokens.FirstOrDefault(r => r.Token == request.RefreshToken);
        if (existing is null || !existing.IsActive)
            return Unauthorized(new { message = "Refresh token is invalid or expired." });

        var user = await _userManager.FindByIdAsync(existing.UserId.ToString());
        if (user is null || user.Status == UserStatus.Inactive)
            return Unauthorized(new { message = "Account is no longer active." });

        existing.RevokedAtUtc = DateTime.UtcNow;
        return await IssueTokens(user);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword(ChangePasswordDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = userId is null ? null : await _userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized(new { message = "Invalid session." });

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Échec du changement de mot de passe pour l'utilisateur : {Email}", user.Email);
            return BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });
        }

        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Mot de passe modifié avec succès pour l'utilisateur : {Email}", user.Email);
        return Ok(new { message = "Password changed successfully." });
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth-policy")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is not null && user.Status != UserStatus.Inactive)
        {
            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            user.PasswordResetCode = code;
            user.PasswordResetCodeExpiresAtUtc = DateTime.UtcNow.AddMinutes(15);
            await _userManager.UpdateAsync(user);

            try
            {
                await _emailService.SendPasswordResetCodeEmailAsync(user.Email!, $"{user.FirstName} {user.LastName}", code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
            }
        }

        // Always return a generic success message — never reveal whether the email exists.
        return Ok(new { message = "If that email exists, a reset code has been sent." });
    }

    

    
    [HttpPost("reset-password")]
    [EnableRateLimiting("auth-policy")]
    public async Task<IActionResult> ResetPassword(ResetPasswordWithCodeDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        var codeValid = user is not null
            && user.PasswordResetCode == request.Code
            && user.PasswordResetCodeExpiresAtUtc is not null
            && user.PasswordResetCodeExpiresAtUtc > DateTime.UtcNow;

        if (!codeValid)
        {
            _logger.LogWarning("Tentative de réinitialisation de mot de passe échouée (code invalide ou expiré) pour : {Email}", request.Email);
            return BadRequest(new { message = "Invalid or expired code." });
        }

        await _userManager.RemovePasswordAsync(user!);
        var result = await _userManager.AddPasswordAsync(user!, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });

        user!.PasswordResetCode = null;
        user.PasswordResetCodeExpiresAtUtc = null;
        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Réinitialisation réussie du mot de passe par code pour : {Email}", user.Email);
        return Ok(new { message = "Password reset successfully." });
    }



    private async Task<ActionResult<LoginResponseDto>> IssueTokens(ApplicationUser user)
    {
        var (accessToken, expiresAtUtc) = _tokenService.CreateAccessToken(user);
        var refreshToken = _tokenService.CreateRefreshToken(user.Id);
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        var claims = new JwtClaimsDto(
            user.Id.ToString(),
            user.Email ?? "",
            user.FirstName,
            user.LastName,
            user.Role,
            user.Initials,
            user.MustChangePassword,
            user.IdStellantis,
            user.ProfilePhotoUrl,
            user.Status,
            user.EmailStellantis,
            user.AltenId,
            user.LanguagesList
        );
        var expiresAtMs = new DateTimeOffset(expiresAtUtc).ToUnixTimeMilliseconds();
        return new LoginResponseDto(accessToken, refreshToken.Token, expiresAtMs, claims);
    }
}
