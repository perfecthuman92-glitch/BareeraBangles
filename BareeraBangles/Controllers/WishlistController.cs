using BareeraBangles.Services;
using Microsoft.AspNetCore.Mvc;

namespace BareeraBangles.Controllers;

public class WishlistController : Controller
{
    private readonly IWishlistService _wishlistService;

    public WishlistController(IWishlistService wishlistService)
    {
        _wishlistService = wishlistService;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _wishlistService.GetItemsAsync();
        ViewData["Title"] = "Wishlist";
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int productId)
    {
        var (success, message, count) = await _wishlistService.ToggleAsync(productId);
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success, message, count });
        }

        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int productId)
    {
        await _wishlistService.RemoveAsync(productId);
        TempData["Success"] = "Removed from wishlist.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Count()
    {
        return Json(new { count = await _wishlistService.GetCountAsync() });
    }
}
