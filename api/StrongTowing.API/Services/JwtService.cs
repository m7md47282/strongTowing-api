using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using StrongTowing.Core.Entities;

namespace StrongTowing.API.Services;

public interface IJwtService
{
    string GenerateToken(ApplicationUser user, IList<string> roles);
    DateTime GetExpirationTime();
}

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(ApplicationUser user, IList<string> roles)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] 
            ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var issuer = jwtSettings["Issuer"] ?? "StrongTowingAPI";
        var audience = jwtSettings["Audience"] ?? "StrongTowingClient";
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim("fullName", user.FullName),
            new Claim("isActive", user.IsActive.ToString())
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public DateTime GetExpirationTime()
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");
        return DateTime.UtcNow.AddMinutes(expirationMinutes);
    }
}

