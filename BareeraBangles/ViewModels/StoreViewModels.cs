using System.ComponentModel.DataAnnotations;
using BareeraBangles.Models;

namespace BareeraBangles.ViewModels;

public class ProductCardViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = "/images/products/placeholder.svg";
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public ProductAgeGroup AgeGroup { get; set; }
    public IEnumerable<string> AvailableSizes { get; set; } = Enumerable.Empty<string>();
    public bool InStock { get; set; }
    public bool HasDiscount => DiscountPrice.HasValue && DiscountPrice < Price;
    public decimal EffectivePrice => DiscountPrice ?? Price;
}

public class ProductDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public ProductAgeGroup AgeGroup { get; set; }
    public List<ProductImageViewModel> Images { get; set; } = new();
    public List<ProductSizeViewModel> Sizes { get; set; } = new();
    public List<ProductCardViewModel> RelatedProducts { get; set; } = new();
    public List<ProductCardViewModel> RecentlyViewed { get; set; } = new();
    public List<BareeraBangles.Services.ReviewViewModel> Reviews { get; set; } = new();
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public bool HasDiscount => DiscountPrice.HasValue && DiscountPrice < Price;
    public decimal EffectivePrice => DiscountPrice ?? Price;
    public bool InStock => Sizes.Any(s => s.StockQuantity > 0 && s.IsActive);
}

public class ProductImageViewModel
{
    public string ImageUrl { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

public class ProductSizeViewModel
{
    public int Id { get; set; }
    public string Size { get; set; } = string.Empty;
    public SizeCategory SizeCategory { get; set; }
    public string? Measurement { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
}

public class ShopViewModel
{
    public List<ProductCardViewModel> Products { get; set; } = new();
    public List<CategoryFilterItem> Categories { get; set; } = new();
    public List<string> AvailableSizes { get; set; } = new();
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Size { get; set; }
    public string? AgeGroup { get; set; }
    public string Sort { get; set; } = "newest";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class CategoryFilterItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class CartViewModel
{
    public List<CartItemViewModel> Items { get; set; } = new();
    public decimal GrandTotal => Items.Sum(i => i.Subtotal);
    public int TotalQuantity => Items.Sum(i => i.Quantity);
}

public class CartItemViewModel
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string SelectedSize { get; set; } = string.Empty;
    public int ProductSizeId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal => UnitPrice * Quantity;
    public int MaxStock { get; set; }
}

public class CheckoutViewModel
{
    [Required, Display(Name = "Full Name"), MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, Display(Name = "Phone Number"), MaxLength(30)]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress, MaxLength(200)]
    public string? Email { get; set; }

    [Required, Display(Name = "Shipping Address"), MaxLength(300)]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Display(Name = "Postal Code"), MaxLength(20)]
    public string? PostalCode { get; set; }

    [Display(Name = "Order Notes"), MaxLength(1000)]
    public string? OrderNotes { get; set; }

    [Required, Display(Name = "Payment Method")]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.JazzCash;

    [Display(Name = "Coupon Code"), MaxLength(40)]
    public string? CouponCode { get; set; }

    [Display(Name = "This is a gift")]
    public bool IsGift { get; set; }

    [Display(Name = "Gift message"), MaxLength(500)]
    public string? GiftMessage { get; set; }

    public CartViewModel Cart { get; set; } = new();
    public decimal ShippingFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FreeShippingThreshold { get; set; }
    public decimal GrandTotal => Math.Max(0, Cart.GrandTotal - DiscountAmount) + ShippingFee;
}

public class OrderViewModel
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public string? OrderNotes { get; set; }
    public string? GiftMessage { get; set; }
    public bool IsGift { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? CouponCode { get; set; }
    public decimal GrandTotal { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemViewModel> Items { get; set; } = new();
    public string? PaymentMessage { get; set; }
    public bool RequiresGatewayConfiguration { get; set; }
}

public class OrderItemViewModel
{
    public string ProductName { get; set; } = string.Empty;
    public string SelectedSize { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? ProductImageUrl { get; set; }
}

public class HomeViewModel
{
    public List<ProductCardViewModel> FeaturedProducts { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<ProductCardViewModel> GalleryProducts { get; set; } = new();
    public List<ProductCardViewModel> RecentlyViewed { get; set; } = new();
    public List<TestimonialViewModel> Testimonials { get; set; } = new();
}

public class TestimonialViewModel
{
    public string Quote { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? ProductName { get; set; }
}

public class AddToCartRequest
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    public int ProductSizeId { get; set; }

    [Range(1, 99)]
    public int Quantity { get; set; } = 1;
}

public class QuickViewViewModel
{
    public ProductDetailsViewModel Product { get; set; } = new();
}
