using Sparks.Api.Dtos;
using Sparks.Api.Models;
using Sparks.Api.Services;
using Xunit;

namespace Sparks.Api.Tests;

/// <summary>
/// The blind-pool guarantee is that an engineer browsing the pool can never see a ticket's
/// title or description before accepting it. These tests pin that contract at two levels:
/// the DTO shape itself, and the mapper that builds it from a real ticket.
/// </summary>
public class BlindDtoTests
{
    [Fact]
    public void TicketBlindDto_HasNoTitleOrDescriptionProperty()
    {
        var properties = typeof(TicketBlindDto).GetProperties();

        Assert.DoesNotContain(properties, p => p.Name.Contains("Title", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, p => p.Name.Contains("Description", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ToBlindDto_NeverLeaksTitleOrDescriptionText()
    {
        var ticket = new Ticket
        {
            ReferenceKZ = "TKT-1001",
            SourceSystemId = Guid.NewGuid(),
            Application = "CATIA",
            Title = "SECRET-TITLE-should-not-leak",
            Description = "SECRET-DESCRIPTION-should-not-leak",
            Priority = TicketPriority.High,
            Status = TicketStatus.New,
            SlaDeadlineUtc = DateTime.UtcNow.AddHours(4),
        };

        var dto = ticket.ToBlindDto();
        var serialized = System.Text.Json.JsonSerializer.Serialize(dto);

        Assert.DoesNotContain("SECRET-TITLE-should-not-leak", serialized);
        Assert.DoesNotContain("SECRET-DESCRIPTION-should-not-leak", serialized);
    }
}
