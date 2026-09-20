using BareeraBangles.Data;
using BareeraBangles.Models;
using BareeraBangles.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BareeraBangles.Services;

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _db;

    public ProductService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<HomeViewModel> GetHomeAsync()
    {
        var featured = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Sizes)
            .Where(p => p.IsActive && p.IsFeatured)
            .OrderByDescending(p => p.CreatedAt)
            .Take(8)
            .ToListAsync();

        var categories = await _db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        var gallery = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Sizes)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Take(8)
            .ToListAsync();

        var reviews = await _db.ProductReviews
            .AsNoTracking()
            .Include(r => r.Product)
            .Where(r => r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .Take(6)
            .ToListAsync();

        return new HomeViewModel
        {
            FeaturedProducts = featured.Select(MapCard).ToList(),
            Categories = categories,
            GalleryProducts = gallery.Select(MapCard).ToList(),
            Testimonials = reviews.Select(r => new TestimonialViewModel
            {
                Quote = r.Comment,
                Author = string.IsNullOrWhiteSpace(r.ReviewerName) ? "Customer" : r.ReviewerName,
                ProductName = r.Product?.Name
            }).ToList()
        };
    }

    public async Task<ShopViewModel> GetShopAsync(string? search, int? categoryId, decimal? minPrice, decimal? maxPrice, string? size, string? ageGroup, string sort, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 6, 48);

        var query = _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Sizes)
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.Name.Contains(term) || p.Description.Contains(term));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => (p.DiscountPrice ?? p.Price) >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => (p.DiscountPrice ?? p.Price) <= maxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(size))
        {
            query = query.Where(p => p.Sizes.Any(s => s.IsActive && s.Size == size && s.StockQuantity > 0));
        }

        if (!string.IsNullOrWhiteSpace(ageGroup) && Enum.TryParse<ProductAgeGroup>(ageGroup, true, out var age))
        {
            query = query.Where(p => p.AgeGroup == age || p.AgeGroup == ProductAgeGroup.Unisex);
        }

        query = sort switch
        {
            "price-asc" => query.OrderBy(p => p.DiscountPrice ?? p.Price),
            "price-desc" => query.OrderByDescending(p => p.DiscountPrice ?? p.Price),
            "name" => query.OrderBy(p => p.Name),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var total = await query.CountAsync();
        var products = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var categories = await _db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryFilterItem
            {
                Id = c.Id,
                Name = c.Name,
                Count = c.Products.Count(p => p.IsActive)
            })
            .ToListAsync();

        var availableSizes = await _db.ProductSizes
            .AsNoTracking()
            .Where(s => s.IsActive && s.StockQuantity > 0)
            .Select(s => s.Size)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();

        return new ShopViewModel
        {
            Products = products.Select(MapCard).ToList(),
            Categories = categories,
            AvailableSizes = availableSizes,
            Search = search,
            CategoryId = categoryId,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            Size = size,
            AgeGroup = ageGroup,
            Sort = sort,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<ProductDetailsViewModel?> GetBySlugAsync(string slug)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Sizes)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

        return product is null ? null : await MapDetailsAsync(product);
    }

    public async Task<ProductDetailsViewModel?> GetByIdAsync(int id)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Sizes)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        return product is null ? null : await MapDetailsAsync(product);
    }

    public Task<ProductDetailsViewModel?> GetQuickViewAsync(int id) => GetByIdAsync(id);

    public async Task<List<Category>> GetActiveCategoriesAsync()
    {
        return await _db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();
    }

    public async Task<List<ProductCardViewModel>> SuggestAsync(string term, int take = 8)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
        {
            return new List<ProductCardViewModel>();
        }

        var q = term.Trim().ToLowerInvariant();
        var products = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Sizes)
            .Where(p => p.IsActive && (p.Name.ToLower().Contains(q) || p.Category!.Name.ToLower().Contains(q)))
            .OrderBy(p => p.Name)
            .Take(take)
            .ToListAsync();

        return products.Select(MapCard).ToList();
    }

    private async Task<ProductDetailsViewModel> MapDetailsAsync(Product product)
    {
        var related = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Sizes)
            .Where(p => p.IsActive && p.CategoryId == product.CategoryId && p.Id != product.Id)
            .Take(4)
            .ToListAsync();

        return new ProductDetailsViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            Price = product.Price,
            DiscountPrice = product.DiscountPrice,
            CategoryName = product.Category?.Name ?? string.Empty,
            CategoryId = product.CategoryId,
            AgeGroup = product.AgeGroup,
            Images = product.Images.OrderBy(i => i.DisplayOrder).Select(i => new ProductImageViewModel
            {
                ImageUrl = i.ImageUrl,
                AltText = i.AltText ?? product.Name,
                IsPrimary = i.IsPrimary
            }).ToList(),
            Sizes = product.Sizes.Where(s => s.IsActive).OrderBy(s => s.SizeCategory).ThenBy(s => s.Size)
                .Select(s => new ProductSizeViewModel
                {
                    Id = s.Id,
                    Size = s.Size,
                    SizeCategory = s.SizeCategory,
                    Measurement = s.Measurement,
                    StockQuantity = s.StockQuantity,
                    IsActive = s.IsActive
                }).ToList(),
            RelatedProducts = related.Select(MapCard).ToList()
        };
    }

    public static ProductCardViewModel MapCard(Product product)
    {
        var primary = product.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayOrder).FirstOrDefault();
        return new ProductCardViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            ImageUrl = primary?.ImageUrl ?? "/images/products/placeholder.svg",
            Price = product.Price,
            DiscountPrice = product.DiscountPrice,
            CategoryName = product.Category?.Name ?? string.Empty,
            AgeGroup = product.AgeGroup,
            AvailableSizes = product.Sizes.Where(s => s.IsActive && s.StockQuantity > 0).Select(s => s.Size).Take(6),
            InStock = product.Sizes.Any(s => s.IsActive && s.StockQuantity > 0)
        };
    }
}
