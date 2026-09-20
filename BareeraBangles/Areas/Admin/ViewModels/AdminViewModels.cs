using System.ComponentModel.DataAnnotations;
using BareeraBangles.Models;

namespace BareeraBangles.Areas.Admin.ViewModels;

public class AdminProductListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
    public int SizeCount { get; set; }
    public int StockTotal { get; set; }
}

public class AdminProductEditViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 1000000)]
    public decimal Price { get; set; }

    public decimal? DiscountPrice { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public ProductAgeGroup AgeGroup { get; set; } = ProductAgeGroup.Adult;

    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;

    public string? PrimaryImageUrl { get; set; }
    public string? SecondaryImageUrl { get; set; }

    public List<AdminSizeEditItem> Sizes { get; set; } = new();
}

public class AdminSizeEditItem
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Size { get; set; } = string.Empty;

    public SizeCategory SizeCategory { get; set; } = SizeCategory.Adult;

    [MaxLength(100)]
    public string? Measurement { get; set; }

    [Range(0, 100000)]
    public int StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;

    public bool Remove { get; set; }
}

public class AdminCategoryEditViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Description { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ImageUrl { get; set; }
}
