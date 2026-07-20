using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace AuthServer.Core.Tokens;
public class AccessTokenGenerator
{
    private readonly JwtSettings _settings;

    public AccessTokenGenerator(JwtSettings settings)
    {
        _settings = settings;
    }

    public string GenerateToken(string userId, string username, SecurityKey signingKey)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_settings.AccessTokenMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}