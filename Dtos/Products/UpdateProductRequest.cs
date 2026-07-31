using System.ComponentModel.DataAnnotations;

namespace JwtSecurityApi.Dtos.Products;

public sealed class UpdateProductRequest
{
    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 999999999)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }
}
