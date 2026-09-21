namespace Sparks.Api.Models;

/// <summary>Mirrors the external/legacy "Applications" catalog table.</summary>
public class Application
{
    public Guid ApplicationId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public Guid? PerimeterId { get; set; }
}

/// <summary>Mirrors the external/legacy "UserApplication" table: a user's own priority ranking for an
/// application. Composite key (ApplicationId, UserId).</summary>
public class UserApplication
{
    public Guid ApplicationId { get; set; }
    public Application? Application { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public int UserDefinedPriority { get; set; }
}
