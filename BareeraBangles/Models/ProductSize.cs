using System.ComponentModel.DataAnnotations;

namespace BareeraBangles.Models;

public class ProductSize
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Required, MaxLength(50)]
    public string Size { get; set; } = string.Empty;

    public SizeCategory SizeCategory { get; set; } = SizeCategory.Adult;

    [MaxLength(100)]
    public string? Measurement { get; set; }

    public int StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;
}
