using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Sparks.Api.Models;

namespace Sparks.Api.Services;

public class TokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public (string token, DateTime expiresAtUtc) CreateAccessToken(ApplicationUser user)
    {
        var jwtSection = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var minutes = int.Parse(jwtSection["AccessTokenMinutes"]!);
        var expires = DateTime.UtcNow.AddMinutes(minutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("firstName", user.FirstName),
            new("lastName", user.LastName),
            new("initials", user.Initials),
        };

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public RefreshToken CreateRefreshToken(Guid userId)
    {
        var days = int.Parse(_config.GetSection("Jwt")["RefreshTokenDays"]!);
        var bytes = RandomNumberGenerator.GetBytes(64);
        return new RefreshToken
        {
            UserId = userId,
            Token = Convert.ToBase64String(bytes),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(days),
        };
    }
}
