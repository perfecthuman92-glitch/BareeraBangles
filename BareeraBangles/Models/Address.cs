using System.ComponentModel.DataAnnotations;

namespace BareeraBangles.Models;

public class Address
{
    public int Id { get; set; }

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Required, MaxLength(300)]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PostalCode { get; set; }
}
