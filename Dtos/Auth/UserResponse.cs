using JwtSecurityApi.Models;

namespace JwtSecurityApi.Dtos.Auth;

public sealed record UserResponse(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    DateTime CreatedAtUtc)
{
    public static UserResponse FromEntity(AppUser user) =>
        new(user.Id, user.FullName, user.Email, user.Role, user.CreatedAtUtc);
}
