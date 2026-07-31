using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JwtSecurityApi.Models;
using JwtSecurityApi.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace JwtSecurityApi.Services;

public sealed class JwtTokenService(IOptions<JwtOptions> jwtOptions)
    : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public JwtTokenResult CreateToken(AppUser user)
    {
        var issuedAtUtc = DateTime.UtcNow;
        var expiresAtUtc = issuedAtUtc.AddMinutes(_jwtOptions.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.UniqueName, user.FullName),
            new("role", user.Role)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtOptions.Key));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: issuedAtUtc,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var serializedToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new JwtTokenResult(serializedToken, expiresAtUtc);
    }
}
