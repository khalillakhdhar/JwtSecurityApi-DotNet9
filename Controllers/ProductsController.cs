using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using JwtSecurityApi.Constants;
using JwtSecurityApi.Data;
using JwtSecurityApi.Dtos.Products;
using JwtSecurityApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JwtSecurityApi.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public sealed class ProductsController(ApplicationDbContext dbContext)
    : ControllerBase
{
    /// <summary>Lister les produits accessibles a un utilisateur authentifie.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var products = await dbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .Select(product => new ProductResponse(
                product.Id,
                product.Name,
                product.Price,
                product.Stock,
                product.CreatedAtUtc,
                product.CreatedByUserId))
            .ToListAsync(cancellationToken);

        return Ok(products);
    }

    /// <summary>Recuperer un produit par son identifiant.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        return product is null
            ? NotFound()
            : Ok(ProductResponse.FromEntity(product));
    }

    /// <summary>Creer un produit. Reserve au role Admin.</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductResponse>> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Price = request.Price,
            Stock = request.Stock,
            CreatedByUserId = userId
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            ProductResponse.FromEntity(product));
    }

    /// <summary>Modifier un produit existant. Reserve au role Admin.</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:int}")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductResponse>> Update(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        product.Name = request.Name.Trim();
        product.Price = request.Price;
        product.Stock = request.Stock;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ProductResponse.FromEntity(product));
    }

    /// <summary>Supprimer un produit. Reserve au role Admin.</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        dbContext.Products.Remove(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(subject, out userId);
    }
}
