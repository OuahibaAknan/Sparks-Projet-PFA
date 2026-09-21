namespace Sparks.Api.Models;

/// <summary>External system tickets are imported from — e.g. "AIDE", "TOOLSUP".</summary>
public class SourceSystem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string? ApiEndpoint { get; set; }
    public DateTime? LastImportDate { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Functional module within a PLM application (e.g. a CATIA workbench).</summary>
public class Module
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string? Application { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Functional domain a ticket is classified into (e.g. "BOM", "ECR Workflow").</summary>
public class Domain
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Site
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string? Location { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
