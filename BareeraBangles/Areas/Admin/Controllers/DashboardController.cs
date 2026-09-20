using BareeraBangles.Configuration;
using BareeraBangles.Data;
using BareeraBangles.Models;
using BareeraBangles.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BareeraBangles.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly StoreSettings _settings;

    public DashboardController(ApplicationDbContext db, IOptions<StoreSettings> settings)
    {
        _db = db;
        _settings = settings.Value;
    }

    public async Task<IActionResult> Index()
    {
        var threshold = _settings.LowStockThreshold;
        var paidLike = new[] { OrderStatus.Paid, OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Delivered };

        ViewBag.ProductCount = await _db.Products.CountAsync();
        ViewBag.OrderCount = await _db.Orders.CountAsync();
        ViewBag.PendingOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.PaymentPending);
        ViewBag.CategoryCount = await _db.Categories.CountAsync();
        ViewBag.Revenue = await _db.Orders
            .Where(o => paidLike.Contains(o.Status))
            .SumAsync(o => (decimal?)o.GrandTotal) ?? 0;
        ViewBag.MonthRevenue = await _db.Orders
            .Where(o => paidLike.Contains(o.Status) && o.CreatedAt >= DateTime.UtcNow.AddDays(-30))
            .SumAsync(o => (decimal?)o.GrandTotal) ?? 0;
        ViewBag.SubscriberCount = await _db.NewsletterSubscribers.CountAsync(s => s.IsActive);
        ViewBag.PendingReviews = await _db.ProductReviews.CountAsync(r => !r.IsApproved);
        ViewBag.StockAlertCount = await _db.StockAlerts.CountAsync(a => !a.IsNotified);

        ViewBag.RecentOrders = await _db.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Take(8)
            .ToListAsync();

        ViewBag.LowStock = await _db.ProductSizes
            .AsNoTracking()
            .Include(s => s.Product)
            .Where(s => s.IsActive && s.StockQuantity <= threshold && s.Product!.IsActive)
            .OrderBy(s => s.StockQuantity)
            .Take(15)
            .ToListAsync();

        ViewBag.TopProducts = await _db.OrderItems
            .AsNoTracking()
            .GroupBy(i => i.ProductName)
            .Select(g => new TopProductRow
            {
                Name = g.Key,
                Qty = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.TotalPrice)
            })
            .OrderByDescending(x => x.Qty)
            .Take(5)
            .ToListAsync();

        ViewBag.LowStockThreshold = threshold;
        ViewData["Title"] = "Admin Dashboard";
        return View();
    }
}

public class TopProductRow
{
    public string Name { get; set; } = string.Empty;
    public int Qty { get; set; }
    public decimal Revenue { get; set; }
}

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class StockAlertsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailService _email;
    private readonly StoreSettings _store;

    public StockAlertsController(
        ApplicationDbContext db,
        IEmailService email,
        IOptions<StoreSettings> store)
    {
        _db = db;
        _email = email;
        _store = store.Value;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Stock Alerts";
        var items = await _db.StockAlerts
            .Include(a => a.Product)
            .Include(a => a.ProductSize)
            .Where(a => !a.IsNotified)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNotified(int id)
    {
        var alert = await _db.StockAlerts
            .Include(a => a.Product)
            .Include(a => a.ProductSize)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (alert is not null)
        {
            var sizeLabel = alert.ProductSize?.Size ?? "selected";
            var body = $"<p>Good news — <strong>{System.Net.WebUtility.HtmlEncode(alert.Product?.Name)}</strong> " +
                       $"(size {System.Net.WebUtility.HtmlEncode(sizeLabel)}) is back in stock at {_store.StoreName}.</p>" +
                       $"<p><a href=\"/product/{alert.Product?.Slug}\">View product</a></p>";
            try
            {
                await _email.SendAsync(alert.Email, $"Back in stock: {alert.Product?.Name}", body);
            }
            catch
            {
                // still mark notified so admin can clear the queue
            }

            alert.IsNotified = true;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Customer notified (email sent or saved to App_Data/mail).";
        }
        return RedirectToAction(nameof(Index));
    }
}
