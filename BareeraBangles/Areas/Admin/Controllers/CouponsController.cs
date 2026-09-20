using BareeraBangles.Data;
using BareeraBangles.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BareeraBangles.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CouponsController : Controller
{
    private readonly ApplicationDbContext _db;

    public CouponsController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Coupons";
        return View(await _db.Coupons.OrderByDescending(c => c.Id).ToListAsync());
    }

    public IActionResult Create()
    {
        ViewData["Title"] = "Add Coupon";
        return View(new Coupon { IsActive = true, PercentOff = 10 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Coupon model)
    {
        model.Code = model.Code.Trim().ToUpperInvariant();
        if (await _db.Coupons.AnyAsync(c => c.Code == model.Code))
        {
            ModelState.AddModelError(nameof(model.Code), "Code already exists.");
        }

        if (!ModelState.IsValid) return View(model);

        _db.Coupons.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Coupon created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var coupon = await _db.Coupons.FindAsync(id);
        if (coupon is null) return NotFound();
        coupon.IsActive = !coupon.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = coupon.IsActive ? "Coupon activated." : "Coupon deactivated.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Subscribers()
    {
        ViewData["Title"] = "Newsletter";
        return View(await _db.NewsletterSubscribers.OrderByDescending(s => s.SubscribedAt).ToListAsync());
    }
}
