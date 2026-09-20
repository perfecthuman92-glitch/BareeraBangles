using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BareeraBangles.Models;

public class Product
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(220)]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DiscountPrice { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public ProductAgeGroup AgeGroup { get; set; } = ProductAgeGroup.Adult;

    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductSize> Sizes { get; set; } = new List<ProductSize>();

    [NotMapped]
    public decimal EffectivePrice => DiscountPrice ?? Price;

    [NotMapped]
    public bool HasDiscount => DiscountPrice.HasValue && DiscountPrice < Price;
}
