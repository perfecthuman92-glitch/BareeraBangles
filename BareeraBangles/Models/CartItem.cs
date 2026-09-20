using System.ComponentModel.DataAnnotations.Schema;

namespace BareeraBangles.Models;

public class CartItem
{
    public int Id { get; set; }

    public int CartId { get; set; }
    public Cart? Cart { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int ProductSizeId { get; set; }
    public ProductSize? ProductSize { get; set; }

    public string SelectedSize { get; set; } = string.Empty;

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [NotMapped]
    public decimal Subtotal => UnitPrice * Quantity;
}
