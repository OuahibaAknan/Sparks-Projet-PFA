using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sparks.Api.Data;
using Sparks.Api.Dtos;
using Sparks.Api.Models;

namespace Sparks.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private static readonly string[] AllowedPhotoExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxPhotoSizeBytes = 5 * 1024 * 1024;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ProfileController(UserManager<ApplicationUser> userManager, AppDbContext db, IWebHostEnvironment env)
    {
        _userManager = userManager;
        _db = db;
        _env = env;
    }

    [HttpPut]
    public async Task<ActionResult<JwtClaimsDto>> UpdateProfile(UpdateProfileDto request)
    {
        var user = await CurrentUserAsync();
        if (user is null) return Unauthorized(new { message = "Invalid session." });

        if (user.Role == Models.UserRole.Admin && !request.IsActive)
        {
            var activeAdminCount = await _db.Users.CountAsync(u => u.Role == Models.UserRole.Admin && u.Status == UserStatus.Active);
            if (activeAdminCount <= 1)
                return BadRequest(new { message = "You are the last active Admin and cannot deactivate your own account." });
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.IdStellantis = request.IdStellantis;
        user.EmailStellantis = request.EmailStellantis;
        user.AltenId = request.AltenId;
        user.SetLanguages(request.Languages);
        user.Status = request.IsActive ? UserStatus.Active : UserStatus.Inactive;

        await _userManager.UpdateAsync(user);

        return ToClaimsDto(user);
    }

    [HttpPost("photo")]
    public async Task<ActionResult<JwtClaimsDto>> UploadPhoto(IFormFile file)
    {
        var user = await CurrentUserAsync();
        if (user is null) return Unauthorized(new { message = "Invalid session." });

        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        if (file.Length > MaxPhotoSizeBytes)
            return BadRequest(new { message = "Photo must be smaller than 5 MB." });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedPhotoExtensions.Contains(extension))
            return BadRequest(new { message = "Only JPG, PNG or WEBP images are allowed." });

        var uploadsDir = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "avatars");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{user.Id}{extension}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        user.ProfilePhotoUrl = $"/uploads/avatars/{fileName}";
        await _userManager.UpdateAsync(user);

        return ToClaimsDto(user);
    }

    private async Task<ApplicationUser?> CurrentUserAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId is null ? null : await _userManager.FindByIdAsync(userId);
    }

    private static JwtClaimsDto ToClaimsDto(ApplicationUser user) => new(
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
}
