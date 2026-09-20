using System.ComponentModel.DataAnnotations;
using BareeraBangles.Data;
using BareeraBangles.Models;
using BareeraBangles.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BareeraBangles.Controllers;

public class SeoController : Controller
{
    private readonly ApplicationDbContext _db;

    public SeoController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("/robots.txt")]
    public ContentResult Robots()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var body = $"User-agent: *\nAllow: /\nDisallow: /Admin\nDisallow: /Identity\nSitemap: {baseUrl}/sitemap.xml\n";
        return Content(body, "text/plain");
    }

    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> Sitemap()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var slugs = await _db.Products.AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => p.Slug)
            .ToListAsync();

        var urls = new List<string>
        {
            $"{baseUrl}/",
            $"{baseUrl}/shop",
            $"{baseUrl}/Home/About",
            $"{baseUrl}/Home/Contact",
            $"{baseUrl}/Home/SizeGuide",
            $"{baseUrl}/Home/Faq",
            $"{baseUrl}/Order/Track"
        };
        urls.AddRange(slugs.Select(s => $"{baseUrl}/product/{s}"));

        var xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                  "<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n" +
                  string.Join("\n", urls.Select(u =>
                      $"  <url><loc>{System.Security.SecurityElement.Escape(u)}</loc></url>")) +
                  "\n</urlset>";

        return Content(xml, "application/xml");
    }
}

public class StockAlertController : Controller
{
    private readonly ApplicationDbContext _db;

    public StockAlertController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Notify(StockAlertRequest model, string slug)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Email))
        {
            TempData["Error"] = "Enter a valid email for stock alerts.";
            return RedirectToAction("Details", "Product", new { slug });
        }

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == model.ProductId && p.IsActive);
        if (product is null) return NotFound();

        var email = model.Email.Trim().ToLowerInvariant();
        var exists = await _db.StockAlerts.AnyAsync(a =>
            a.Email == email &&
            a.ProductId == model.ProductId &&
            a.ProductSizeId == model.ProductSizeId &&
            !a.IsNotified);

        if (!exists)
        {
            _db.StockAlerts.Add(new StockAlert
            {
                ProductId = model.ProductId,
                ProductSizeId = model.ProductSizeId,
                Email = email
            });
            await _db.SaveChangesAsync();
        }

        TempData["Success"] = "We'll email you when this size is back in stock.";
        return RedirectToAction("Details", "Product", new { slug });
    }
}

public class StockAlertRequest
{
    public int ProductId { get; set; }
    public int? ProductSizeId { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}
