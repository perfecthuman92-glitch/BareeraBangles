using BareeraBangles.Data;
using BareeraBangles.Models;
using BareeraBangles.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BareeraBangles.Services;

public class CartService : ICartService
{
    private const string SessionKey = "BareeraCartSessionId";
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CartService(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<CartViewModel> GetCartAsync()
    {
        var cart = await GetOrCreateCartAsync();
        await _db.Entry(cart).Collection(c => c.Items).Query()
            .Include(i => i.Product)!.ThenInclude(p => p!.Images)
            .Include(i => i.ProductSize)
            .LoadAsync();

        return new CartViewModel
        {
            Items = cart.Items.Select(i => new CartItemViewModel
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? "Product",
                ProductSlug = i.Product?.Slug ?? string.Empty,
                ImageUrl = i.Product?.Images.OrderByDescending(img => img.IsPrimary).FirstOrDefault()?.ImageUrl
                           ?? "/images/products/placeholder.svg",
                SelectedSize = i.SelectedSize,
                ProductSizeId = i.ProductSizeId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                MaxStock = i.ProductSize?.StockQuantity ?? i.Quantity
            }).ToList()
        };
    }

    public async Task<(bool Success, string Message)> AddAsync(int productId, int productSizeId, int quantity)
    {
        if (quantity < 1)
        {
            return (false, "Quantity must be at least 1.");
        }

        var product = await _db.Products
            .Include(p => p.Sizes)
            .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);

        if (product is null)
        {
            return (false, "Product not found.");
        }

        var size = product.Sizes.FirstOrDefault(s => s.Id == productSizeId && s.IsActive);
        if (size is null)
        {
            return (false, "Please select an available size.");
        }

        if (size.StockQuantity < 1)
        {
            return (false, "Selected size is out of stock.");
        }

        var cart = await GetOrCreateCartAsync();
        await _db.Entry(cart).Collection(c => c.Items).LoadAsync();

        var existing = cart.Items.FirstOrDefault(i => i.ProductId == productId && i.ProductSizeId == productSizeId);
        var newQty = (existing?.Quantity ?? 0) + quantity;
        if (newQty > size.StockQuantity)
        {
            return (false, $"Only {size.StockQuantity} available for size {size.Size}.");
        }

        if (existing is null)
        {
            cart.Items.Add(new CartItem
            {
                ProductId = productId,
                ProductSizeId = productSizeId,
                SelectedSize = size.Size,
                Quantity = quantity,
                UnitPrice = product.EffectivePrice
            });
        }
        else
        {
            existing.Quantity = newQty;
            existing.UnitPrice = product.EffectivePrice;
            existing.SelectedSize = size.Size;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, $"{product.Name} ({size.Size}) added to cart.");
    }

    public async Task<(bool Success, string Message)> UpdateQuantityAsync(int cartItemId, int quantity)
    {
        var cart = await GetOrCreateCartAsync();
        await _db.Entry(cart).Collection(c => c.Items).Query()
            .Include(i => i.ProductSize)
            .LoadAsync();

        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
        if (item is null)
        {
            return (false, "Cart item not found.");
        }

        if (quantity < 1)
        {
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
            return (true, "Item removed from cart.");
        }

        var max = item.ProductSize?.StockQuantity ?? 0;
        if (quantity > max)
        {
            return (false, $"Only {max} available for size {item.SelectedSize}.");
        }

        item.Quantity = quantity;
        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, "Cart updated.");
    }

    public async Task RemoveAsync(int cartItemId)
    {
        var cart = await GetOrCreateCartAsync();
        await _db.Entry(cart).Collection(c => c.Items).LoadAsync();
        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
        if (item is not null)
        {
            _db.CartItems.Remove(item);
            cart.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task ClearAsync()
    {
        var cart = await GetOrCreateCartAsync();
        await _db.Entry(cart).Collection(c => c.Items).LoadAsync();
        _db.CartItems.RemoveRange(cart.Items);
        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<int> GetCountAsync()
    {
        var cart = await GetOrCreateCartAsync();
        return await _db.CartItems.Where(i => i.CartId == cart.Id).SumAsync(i => (int?)i.Quantity) ?? 0;
    }

    private async Task<Cart> GetOrCreateCartAsync()
    {
        var http = _httpContextAccessor.HttpContext
                   ?? throw new InvalidOperationException("No HTTP context available.");

        var session = http.Session;
        await session.LoadAsync();
        var sessionId = session.GetString(SessionKey);
        if (string.IsNullOrEmpty(sessionId))
        {
            sessionId = Guid.NewGuid().ToString("N");
            session.SetString(SessionKey, sessionId);
        }

        var userId = http.User?.Identity?.IsAuthenticated == true
            ? http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            : null;

        var cart = await _db.Carts.FirstOrDefaultAsync(c =>
            (userId != null && c.UserId == userId) || c.SessionId == sessionId);

        if (cart is null)
        {
            cart = new Cart
            {
                SessionId = sessionId,
                UserId = userId
            };
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();
        }
        else if (userId is not null && cart.UserId != userId)
        {
            cart.UserId = userId;
            await _db.SaveChangesAsync();
        }

        return cart;
    }
}
