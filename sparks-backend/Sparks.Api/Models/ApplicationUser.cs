using Microsoft.AspNetCore.Identity;

namespace Sparks.Api.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? IdStellantis { get; set; }
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public UserAvailability Availability { get; set; } = UserAvailability.Available;
    public string AvatarColor { get; set; } = "#008BD2";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool MustChangePassword { get; set; } = true;
    public string? ProfilePhotoUrl { get; set; }
    public string? EmailStellantis { get; set; }
    public string? AltenId { get; set; }
    public string? PasswordResetCode { get; set; }
    public DateTime? PasswordResetCodeExpiresAtUtc { get; set; }
    /// <summary>Comma-separated list of languages the employee speaks (e.g. "French,English") — informational only.</summary>
    public string? Languages { get; set; }

    public Guid? TeamId { get; set; }
    public Team? Team { get; set; }

    public string Initials => $"{(FirstName.Length > 0 ? FirstName[0] : ' ')}{(LastName.Length > 0 ? LastName[0] : ' ')}".ToUpperInvariant();

    public List<string> LanguagesList =>
        string.IsNullOrWhiteSpace(Languages)
            ? new List<string>()
            : Languages.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    public void SetLanguages(List<string>? languages) =>
        Languages = languages is null || languages.Count == 0 ? null : string.Join(",", languages);

    public ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string Token { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }

    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;
}
