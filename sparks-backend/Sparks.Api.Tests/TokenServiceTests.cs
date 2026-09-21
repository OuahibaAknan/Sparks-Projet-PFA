using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Sparks.Api.Models;
using Sparks.Api.Services;
using Xunit;

namespace Sparks.Api.Tests;

/// <summary>
/// TokenService issues the JWTs the whole app trusts for authentication. These tests check both
/// the happy path (claims, expiry) and the security-relevant negative path: a token signed with
/// a different key, or presented to a validator expecting a different issuer/audience, must be
/// rejected — otherwise the signature check would be decorative.
/// </summary>
public class TokenServiceTests
{
    private const string SigningKey = "test-signing-key-at-least-32-characters-long-for-hmacsha256";

    private static TokenService BuildService(string key = SigningKey, string issuer = "SparksApi", string audience = "SparksClient",
        int accessMinutes = 60, int refreshDays = 14)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = key,
            ["Jwt:Issuer"] = issuer,
            ["Jwt:Audience"] = audience,
            ["Jwt:AccessTokenMinutes"] = accessMinutes.ToString(),
            ["Jwt:RefreshTokenDays"] = refreshDays.ToString(),
        }).Build();

        return new TokenService(config);
    }

    private static ApplicationUser SampleUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "marcus.weber@alten.com",
        FirstName = "Marcus",
        LastName = "Weber",
        Role = UserRole.Specialist,
    };

    [Fact]
    public void CreateAccessToken_EmbedsExpectedClaims()
    {
        var user = SampleUser();
        var (token, _) = BuildService().CreateAccessToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(user.Id.ToString(), jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.Email, jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("Specialist", jwt.Claims.Single(c => c.Type == ClaimTypes.Role).Value);
        Assert.Equal("Marcus", jwt.Claims.Single(c => c.Type == "firstName").Value);
        Assert.Equal("MW", jwt.Claims.Single(c => c.Type == "initials").Value);
    }

    [Fact]
    public void CreateAccessToken_ExpiryMatchesConfiguredWindow()
    {
        var before = DateTime.UtcNow;
        var (_, expiresAtUtc) = BuildService(accessMinutes: 45).CreateAccessToken(SampleUser());

        Assert.InRange(expiresAtUtc, before.AddMinutes(45).AddSeconds(-5), before.AddMinutes(45).AddSeconds(5));
    }

    [Fact]
    public void CreateAccessToken_ValidatesSuccessfully_WithMatchingKeyIssuerAndAudience()
    {
        var (token, _) = BuildService().CreateAccessToken(SampleUser());

        var principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
        {
            ValidIssuer = "SparksApi",
            ValidAudience = "SparksClient",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
        }, out _);

        Assert.NotNull(principal);
    }

    [Fact]
    public void CreateAccessToken_FailsValidation_WhenSignedWithADifferentKey()
    {
        var (token, _) = BuildService(key: "another-completely-different-signing-key-32chars-min").CreateAccessToken(SampleUser());

        Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                ValidIssuer = "SparksApi",
                ValidAudience = "SparksClient",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)), // wrong key on purpose
            }, out _));
    }

    [Fact]
    public void CreateAccessToken_FailsValidation_ForAnUntrustedIssuer()
    {
        var (token, _) = BuildService(issuer: "SomeOtherIssuer").CreateAccessToken(SampleUser());

        Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                ValidIssuer = "SparksApi", // the app only trusts this issuer
                ValidAudience = "SparksClient",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            }, out _));
    }

    [Fact]
    public void CreateRefreshToken_ProducesUniqueValuesWithConfiguredExpiry()
    {
        var service = BuildService(refreshDays: 14);
        var userId = Guid.NewGuid();

        var first = service.CreateRefreshToken(userId);
        var second = service.CreateRefreshToken(userId);

        Assert.NotEqual(first.Token, second.Token);
        Assert.Equal(userId, first.UserId);
        Assert.InRange(first.ExpiresAtUtc, DateTime.UtcNow.AddDays(14).AddMinutes(-1), DateTime.UtcNow.AddDays(14).AddMinutes(1));
    }
}
