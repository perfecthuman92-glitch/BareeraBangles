using BareeraBangles.Services;
using Microsoft.AspNetCore.Mvc;

namespace BareeraBangles.Controllers;

public class ProductController : Controller
{
    private readonly IProductService _productService;
    private readonly IRecentlyViewedService _recentlyViewed;
    private readonly IReviewService _reviewService;

    public ProductController(
        IProductService productService,
        IRecentlyViewedService recentlyViewed,
        IReviewService reviewService)
    {
        _productService = productService;
        _recentlyViewed = recentlyViewed;
        _reviewService = reviewService;
    }

    [HttpGet("/shop")]
    public async Task<IActionResult> Index(
        string? search,
        int? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        string? size,
        string? ageGroup,
        string sort = "newest",
        int page = 1)
    {
        var model = await _productService.GetShopAsync(search, categoryId, minPrice, maxPrice, size, ageGroup, sort, page, 12);
        ViewData["Title"] = "Shop";
        ViewData["MetaDescription"] = "Browse Bareera Bangles — gold, bridal, traditional, stone, daily wear, premium, and baby & kids collections.";
        return View(model);
    }

    [HttpGet("/product/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var product = await _productService.GetBySlugAsync(slug);
        if (product is null) return NotFound();

        await _recentlyViewed.TrackAsync(product.Id);
        product.RecentlyViewed = await _recentlyViewed.GetAsync(4, product.Id);
        product.Reviews = await _reviewService.GetApprovedForProductAsync(product.Id);
        var (avg, count) = await _reviewService.GetStatsAsync(product.Id);
        product.AverageRating = avg;
        product.ReviewCount = count;

        ViewData["Title"] = product.Name;
        ViewData["MetaDescription"] = product.Description.Length > 155
            ? product.Description[..152] + "..."
            : product.Description;
        return View(product);
    }

    [HttpGet("/product/quick-view/{id:int}")]
    public async Task<IActionResult> QuickView(int id)
    {
        var product = await _productService.GetQuickViewAsync(id);
        if (product is null) return NotFound();
        return PartialView("_QuickView", product);
    }

    [HttpGet("/product/suggest")]
    public async Task<IActionResult> Suggest(string? q)
    {
        var items = await _productService.SuggestAsync(q ?? string.Empty);
        return Json(items.Select(p => new
        {
            p.Id,
            p.Name,
            p.Slug,
            p.ImageUrl,
            price = p.EffectivePrice,
            p.CategoryName
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(SubmitReviewRequest model, string slug)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please complete the review form.";
            return RedirectToAction(nameof(Details), new { slug });
        }

        var (success, message) = await _reviewService.SubmitAsync(model);
        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Details), new { slug });
    }
}
