using System.Text.Json;
using BareeraBangles.Data;
using BareeraBangles.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BareeraBangles.Services;

public interface IRecentlyViewedService
{
    Task TrackAsync(int productId);
    Task<List<ProductCardViewModel>> GetAsync(int take = 4, int? excludeProductId = null);
}

public class RecentlyViewedService : IRecentlyViewedService
{
    private const string SessionKey = "BareeraRecentlyViewed";
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;

    public RecentlyViewedService(ApplicationDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task TrackAsync(int productId)
    {
        var ids = ReadIds();
        ids.Remove(productId);
        ids.Insert(0, productId);
        if (ids.Count > 12) ids = ids.Take(12).ToList();
        SaveIds(ids);
        await Task.CompletedTask;
    }

    public async Task<List<ProductCardViewModel>> GetAsync(int take = 4, int? excludeProductId = null)
    {
        var ids = ReadIds();
        if (excludeProductId.HasValue) ids.Remove(excludeProductId.Value);
        ids = ids.Take(take).ToList();
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

public interface ICouponService
{
    Task<(bool Success, string Message, decimal Discount, bool FreeShipping)> ValidateAsync(string? code, decimal subtotal);
    Task IncrementRedemptionAsync(string code);
}

public class CouponService : ICouponService
{
    private readonly ApplicationDbContext _db;

    public CouponService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(bool Success, string Message, decimal Discount, bool FreeShipping)> ValidateAsync(string? code, decimal subtotal)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return (true, string.Empty, 0, false);
        }

        var normalized = code.Trim().ToUpperInvariant();
        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == normalized && c.IsActive);
        if (coupon is null)
        {
            return (false, "Invalid coupon code.", 0, false);
        }

        if (coupon.ExpiresAt.HasValue && coupon.ExpiresAt.Value < DateTime.UtcNow)
        {
            return (false, "This coupon has expired.", 0, false);
        }

        if (coupon.MaxRedemptions.HasValue && coupon.RedemptionCount >= coupon.MaxRedemptions.Value)
        {
            return (false, "This coupon has reached its usage limit.", 0, false);
        }

        if (coupon.MinimumSubtotal.HasValue && subtotal < coupon.MinimumSubtotal.Value)
        {
            return (false, $"Minimum order of {coupon.MinimumSubtotal.Value:N0} required for this coupon.", 0, false);
        }

        decimal discount = 0;
        if (coupon.PercentOff > 0)
        {
            discount = Math.Round(subtotal * (coupon.PercentOff / 100m), 2);
        }
        if (coupon.AmountOff > 0)
        {
            discount += coupon.AmountOff;
        }

        discount = Math.Min(discount, subtotal);
        var freeShip = coupon.FreeShipping || (coupon.PercentOff == 0 && coupon.AmountOff == 0 && normalized.Contains("SHIP"));
        var msg = freeShip && discount == 0
            ? $"Coupon {coupon.Code} applied — free shipping."
            : $"Coupon {coupon.Code} applied.";
        return (true, msg, discount, freeShip);
    }

    public async Task IncrementRedemptionAsync(string code)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == normalized);
        if (coupon is null) return;
        coupon.RedemptionCount += 1;
        await _db.SaveChangesAsync();
    }
}
