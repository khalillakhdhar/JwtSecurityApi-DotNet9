using JwtSecurityApi.Models;

namespace JwtSecurityApi.Dtos.Products;

public sealed record ProductResponse(
    int Id,
    string Name,
    decimal Price,
    int Stock,
    DateTime CreatedAtUtc,
    Guid CreatedByUserId)
{
    public static ProductResponse FromEntity(Product product) =>
        new(
            product.Id,
            product.Name,
            product.Price,
            product.Stock,
            product.CreatedAtUtc,
            product.CreatedByUserId);
}
