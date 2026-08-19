using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using JwtSecurityApi.Constants;
using JwtSecurityApi.Data;
using JwtSecurityApi.Dtos.Auth;
using JwtSecurityApi.Models;
using JwtSecurityApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

namespace JwtSecurityApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    ApplicationDbContext dbContext,
    IPasswordHasher<AppUser> passwordHasher,
    IJwtTokenService jwtTokenService) : ControllerBase
{
    /// <summary>Creer un compte utilisateur standard et retourner un token JWT.</summary>
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var normalizedEmail = email.ToUpperInvariant();

        var emailExists = await dbContext.Users.AnyAsync(
            user => user.NormalizedEmail == normalizedEmail,
            cancellationToken);

        if (emailExists)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Adresse e-mail déjà utilisée",
                Detail = "Un compte existe déjà avec cette adresse e-mail.",
                Status = StatusCodes.Status409Conflict
            });
        }

        // Le client ne choisit jamais son rôle : cela empêcherait une élévation
        // de privilèges en envoyant simplement role = Admin dans le JSON.
        var user = new AppUser
        {
            FullName = request.FullName.Trim(),
            Email = email,
            NormalizedEmail = normalizedEmail,
            Role = Roles.User
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = CreateAuthResponse(user);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>Authentifier un utilisateur et retourner un token JWT Bearer.</summary>
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.NormalizedEmail == normalizedEmail,
            cancellationToken);

        if (user is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentification refusée",
                Detail = "E-mail ou mot de passe incorrect.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentification refusée",
                Detail = "E-mail ou mot de passe incorrect.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(CreateAuthResponse(user));
    }

    /// <summary>Retourner le profil de l'utilisateur connecte.</summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(subject, out var userId))
        {
            return Unauthorized();
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        return user is null
            ? Unauthorized()
            : Ok(UserResponse.FromEntity(user));
    }

    private AuthResponse CreateAuthResponse(AppUser user)
    {
        var token = jwtTokenService.CreateToken(user);

        return new AuthResponse(
            token.AccessToken,
            "Bearer",
            token.ExpiresAtUtc,
            UserResponse.FromEntity(user));
    }
}
