using System.Text.Json;
using BareeraBangles.Data;
using BareeraBangles.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BareeraBangles.Services;

public interface ICompareService
{
    Task<List<ProductCardViewModel>> GetAsync();
    Task<(bool Success, string Message, int Count)> ToggleAsync(int productId);
    Task RemoveAsync(int productId);
    Task ClearAsync();
    Task<int> GetCountAsync();
}

public class CompareService : ICompareService
{
    private const string SessionKey = "BareeraCompare";
    private const int MaxItems = 3;
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;

    public CompareService(ApplicationDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task<List<ProductCardViewModel>> GetAsync()
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
        if (!await _db.Products.AnyAsync(p => p.Id == productId && p.IsActive))
        {
            return (false, "Product not found.", ReadIds().Count);
        }

        var ids = ReadIds();
        if (ids.Contains(productId))
        {
            ids.Remove(productId);
            SaveIds(ids);
            return (true, "Removed from compare.", ids.Count);
        }

        if (ids.Count >= MaxItems)
        {
            return (false, $"You can compare up to {MaxItems} products.", ids.Count);
        }

        ids.Add(productId);
        SaveIds(ids);
        return (true, "Added to compare.", ids.Count);
    }

    public Task RemoveAsync(int productId)
    {
        var ids = ReadIds();
        ids.Remove(productId);
        SaveIds(ids);
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        SaveIds(new List<int>());
        return Task.CompletedTask;
    }

    public Task<int> GetCountAsync() => Task.FromResult(ReadIds().Count);

    private List<int> ReadIds()
    {
        var session = _http.HttpContext?.Session ?? throw new InvalidOperationException("No session.");
        session.LoadAsync().GetAwaiter().GetResult();
        var json = session.GetString(SessionKey);
        if (string.IsNullOrEmpty(json)) return new List<int>();
        try { return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>(); }
        catch { return new List<int>(); }
    }

    private void SaveIds(List<int> ids)
    {
        var session = _http.HttpContext?.Session ?? throw new InvalidOperationException("No session.");
        session.SetString(SessionKey, JsonSerializer.Serialize(ids));
    }
}
