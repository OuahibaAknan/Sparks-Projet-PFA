namespace Sparks.Api.Models;

public class Team
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string? Department { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ApplicationUser> Members { get; set; } = new List<ApplicationUser>();
}
