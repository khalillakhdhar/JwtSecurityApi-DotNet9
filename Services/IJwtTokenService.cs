using JwtSecurityApi.Models;

namespace JwtSecurityApi.Services;

public interface IJwtTokenService
{
    JwtTokenResult CreateToken(AppUser user);
}

public sealed record JwtTokenResult(string AccessToken, DateTime ExpiresAtUtc);
