using System.ComponentModel.DataAnnotations;
using Sparks.Api.Models;

namespace Sparks.Api.Dtos;

public record LoginRequestDto([Required, EmailAddress] string Email, [Required] string Password);

public record RefreshRequestDto(string RefreshToken);

public record JwtClaimsDto(
    string Sub,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role,
    string Initials,
    bool MustChangePassword,
    string? IdStellantis,
    string? ProfilePhotoUrl,
    UserStatus Status,
    string? EmailStellantis,
    string? AltenId,
    List<string> Languages
);

public record LoginResponseDto(string AccessToken, string RefreshToken, long ExpiresAt, JwtClaimsDto User);

public record ChangePasswordDto([Required] string CurrentPassword, [Required] string NewPassword);

public record UpdateProfileDto(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    string? IdStellantis,
    string? EmailStellantis,
    string? AltenId,
    List<string>? Languages,
    bool IsActive
);

public record ForgotPasswordDto([Required, EmailAddress] string Email);

public record ResetPasswordWithCodeDto([Required, EmailAddress] string Email, [Required] string Code, [Required] string NewPassword);
