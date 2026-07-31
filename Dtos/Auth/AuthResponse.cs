namespace JwtSecurityApi.Dtos.Auth;

public sealed record AuthResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    UserResponse User);
