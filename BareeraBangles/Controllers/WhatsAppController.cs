using BareeraBangles.Services;
using Microsoft.AspNetCore.Mvc;

namespace BareeraBangles.Controllers;

public class WhatsAppController : Controller
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly IProductService _productService;
    private readonly ICartService _cartService;

    public WhatsAppController(
        IWhatsAppService whatsAppService,
        IProductService productService,
        ICartService cartService)
    {
        _whatsAppService = whatsAppService;
        _productService = productService;
        _cartService = cartService;
    }

    [HttpGet]
    public IActionResult Chat(string? message)
    {
        return Redirect(_whatsAppService.BuildGeneralChatUrl(message));
    }

    [HttpGet]
    public async Task<IActionResult> Product(int id, int? sizeId, int quantity = 1)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product is null) return NotFound();

        var size = sizeId.HasValue
            ? product.Sizes.FirstOrDefault(s => s.Id == sizeId.Value)?.Size
            : null;

        var url = _whatsAppService.BuildProductOrderUrl(
            product.Name,
            size,
            Math.Max(1, quantity),
            product.EffectivePrice);

        return Redirect(url);
    }

    [HttpGet]
    public async Task<IActionResult> Cart()
    {
        var cart = await _cartService.GetCartAsync();
        return Redirect(_whatsAppService.BuildCartOrderUrl(cart));
    }
}
