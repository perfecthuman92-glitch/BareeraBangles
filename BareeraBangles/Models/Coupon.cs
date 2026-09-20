using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BareeraBangles.Models;

public class Coupon
{
    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>Percentage discount (e.g. 10 = 10%). Use 0 if fixed amount.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal PercentOff { get; set; }

    /// <summary>Fixed amount off in store currency.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountOff { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MinimumSubtotal { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public int? MaxRedemptions { get; set; }

    public int RedemptionCount { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>When true, shipping fee is waived for this coupon.</summary>
    public bool FreeShipping { get; set; }
}

public class NewsletterSubscriber
{
    public int Id { get; set; }

    [Required, MaxLength(200), EmailAddress]
    public string Email { get; set; } = string.Empty;

    public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}
