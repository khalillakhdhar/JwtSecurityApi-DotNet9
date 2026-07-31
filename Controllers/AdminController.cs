using JwtSecurityApi.Constants;
using JwtSecurityApi.Data;
using JwtSecurityApi.Dtos.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JwtSecurityApi.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = Policies.AdminOnly)]
public sealed class AdminController(ApplicationDbContext dbContext)
    : ControllerBase
{
    [HttpGet("users")]
    [ProducesResponseType<IReadOnlyList<UserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetUsers(
        CancellationToken cancellationToken)
    {
        var users = await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.FullName)
            .Select(user => new UserResponse(
                user.Id,
                user.FullName,
                user.Email,
                user.Role,
                user.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(users);
    }
}
