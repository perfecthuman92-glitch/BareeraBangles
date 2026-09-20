using System.Text.Json;
using BareeraBangles.Data;
using BareeraBangles.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BareeraBangles.Services;

public class WishlistService : IWishlistService
{
    private const string SessionKey = "BareeraWishlist";
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WishlistService(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<HashSet<int>> GetProductIdsAsync()
    {
        return Task.FromResult(ReadIds().ToHashSet());
    }

    public Task<List<int>> GetOrderedIdsAsync()
    {
        return Task.FromResult(ReadIds());
    }

    private List<int> ReadIds()
    {
        var session = GetSession();
        session.LoadAsync().GetAwaiter().GetResult();
        var json = session.GetString(SessionKey);
        if (string.IsNullOrEmpty(json)) return new List<int>();
        try
        {
            return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
        }
        catch
        {
            return new List<int>();
        }
    }

    public async Task<List<ProductCardViewModel>> GetItemsAsync()
    {
        var ids = ReadIds();
        if (ids.Count == 0) return new List<ProductCardViewModel>();

        var products = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Sizes)
            .Where(p => p.IsActive && ids.Contains(p.Id))
            .ToListAsync();

        var map = products.ToDictionary(p => p.Id);
        return ids.Where(id => map.ContainsKey(id)).Select(id => ProductService.MapCard(map[id])).ToList();
    }

    public async Task<(bool Success, string Message, int Count)> ToggleAsync(int productId)
    {
        var exists = await _db.Products.AnyAsync(p => p.Id == productId && p.IsActive);
        if (!exists) return (false, "Product not found.", ReadIds().Count);

        var ids = ReadIds();
        string message;
        if (ids.Contains(productId))
        {
            ids.Remove(productId);
            message = "Removed from wishlist.";
        }
        else
        {
            ids.Insert(0, productId);
            message = "Added to wishlist.";
        }

        SaveIds(ids);
        return (true, message, ids.Count);
    }

    public Task RemoveAsync(int productId)
    {
        var ids = ReadIds();
        ids.Remove(productId);
        SaveIds(ids);
        return Task.CompletedTask;
    }

    public Task<int> GetCountAsync() => Task.FromResult(ReadIds().Count);

    private void SaveIds(List<int> ids)
    {
        var session = GetSession();
        session.SetString(SessionKey, JsonSerializer.Serialize(ids));
    }

    private ISession GetSession()
    {
        var http = _httpContextAccessor.HttpContext
                   ?? throw new InvalidOperationException("No HTTP context.");
        return http.Session;
    }
}
