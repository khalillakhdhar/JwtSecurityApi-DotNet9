using System.ComponentModel.DataAnnotations;

namespace JwtSecurityApi.Models;

public sealed class Product
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }
    public int Stock { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Guid CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }
}
