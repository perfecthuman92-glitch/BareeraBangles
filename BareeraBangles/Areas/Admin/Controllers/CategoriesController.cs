using BareeraBangles.Areas.Admin.ViewModels;
using BareeraBangles.Data;
using BareeraBangles.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace BareeraBangles.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoriesController : Controller
{
    private readonly ApplicationDbContext _db;

    public CategoriesController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
        ViewData["Title"] = "Categories";
        return View(items);
    }

    public IActionResult Create()
    {
        ViewData["Title"] = "Add Category";
        return View(new AdminCategoryEditViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminCategoryEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var slug = Slugify(model.Name);
        if (await _db.Categories.AnyAsync(c => c.Slug == slug))
        {
            slug += "-" + Guid.NewGuid().ToString("N")[..4];
        }

        _db.Categories.Add(new Category
        {
            Name = model.Name.Trim(),
            Description = model.Description,
            DisplayOrder = model.DisplayOrder,
            IsActive = model.IsActive,
            ImageUrl = model.ImageUrl,
            Slug = slug
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Category created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null) return NotFound();
        ViewData["Title"] = "Edit Category";
        return View(new AdminCategoryEditViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive,
            ImageUrl = category.ImageUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminCategoryEditViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        var category = await _db.Categories.FindAsync(id);
        if (category is null) return NotFound();

        category.Name = model.Name.Trim();
        category.Description = model.Description;
        category.DisplayOrder = model.DisplayOrder;
        category.IsActive = model.IsActive;
        category.ImageUrl = model.ImageUrl;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Category updated.";
        return RedirectToAction(nameof(Index));
    }

    private static string Slugify(string value)
    {
        var slug = value.ToLowerInvariant();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "category" : slug;
    }
}

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrdersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly BareeraBangles.Services.IOrderService _orderService;

    public OrdersController(ApplicationDbContext db, BareeraBangles.Services.IOrderService orderService)
    {
        _db = db;
        _orderService = orderService;
    }

    public async Task<IActionResult> Index()
    {
        var orders = await _orderService.GetAllOrdersAsync();
        ViewData["Title"] = "Orders";
        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv()
    {
        var orders = await _orderService.GetAllOrdersAsync();
        var sb = new StringBuilder();
        sb.AppendLine("OrderNumber,Date,Customer,Phone,City,Status,PaymentMethod,Subtotal,Discount,Shipping,GrandTotal,Coupon,IsGift");
        foreach (var o in orders)
        {
            sb.AppendLine(string.Join(",",
                Csv(o.OrderNumber),
                o.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                Csv(o.FullName),
                Csv(o.PhoneNumber),
                Csv(o.City),
                o.Status,
                o.PaymentMethod,
                o.Subtotal.ToString("0.00"),
                o.DiscountAmount.ToString("0.00"),
                o.ShippingFee.ToString("0.00"),
                o.GrandTotal.ToString("0.00"),
                Csv(o.CouponCode ?? ""),
                o.IsGift));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"bareera-orders-{DateTime.Now:yyyyMMdd}.csv");
    }

    private static string Csv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _orderService.GetOrderEntityAsync(id);
        if (order is null) return NotFound();
        ViewData["Title"] = $"Order {order.OrderNumber}";
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
    {
        await _orderService.UpdateStatusAsync(id, status);
        TempData["Success"] = "Order status updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAdminNotes(int id, string? adminNotes)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order is null) return NotFound();
        order.AdminNotes = string.IsNullOrWhiteSpace(adminNotes) ? null : adminNotes.Trim();
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Admin notes saved.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
