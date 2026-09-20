using System.ComponentModel.DataAnnotations;

namespace BareeraBangles.Models;

public class StockAlert
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int? ProductSizeId { get; set; }
    public ProductSize? ProductSize { get; set; }

    [Required, MaxLength(200), EmailAddress]
    public string Email { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsNotified { get; set; }
}
