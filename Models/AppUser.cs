using System.ComponentModel.DataAnnotations;
using JwtSecurityApi.Constants;

namespace JwtSecurityApi.Models;

public sealed class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(256)]
    public string NormalizedEmail { get; set; } = string.Empty;

    [MaxLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Role { get; set; } = Roles.User;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
