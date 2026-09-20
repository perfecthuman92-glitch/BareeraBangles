using BareeraBangles.Services;
using BareeraBangles.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BareeraBangles.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cartService;
    private readonly IWhatsAppService _whatsAppService;

    public CartController(ICartService cartService, IWhatsAppService whatsAppService)
    {
        _cartService = cartService;
        _whatsAppService = whatsAppService;
    }

    public async Task<IActionResult> Index()
    {
        var cart = await _cartService.GetCartAsync();
        ViewBag.WhatsAppUrl = _whatsAppService.BuildCartOrderUrl(cart);
        ViewData["Title"] = "Your Cart";
        return View(cart);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(AddToCartRequest request)
    {
        if (!ModelState.IsValid || request.ProductSizeId <= 0)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Please select a size before adding to cart." });
            }
            TempData["Error"] = "Please select a size before adding to cart.";
            return RedirectToAction("Index", "Product");
        }

        var (success, message) = await _cartService.AddAsync(request.ProductId, request.ProductSizeId, request.Quantity);
        var count = await _cartService.GetCountAsync();

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success, message, count });
        }

        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, int quantity)
    {
        var (success, message) = await _cartService.UpdateQuantityAsync(id, quantity);
        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id)
    {
        await _cartService.RemoveAsync(id);
        TempData["Success"] = "Item removed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Count()
    {
        var count = await _cartService.GetCountAsync();
        return Json(new { count });
    }
}
