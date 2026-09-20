using BareeraBangles.Configuration;
using BareeraBangles.Services;
using BareeraBangles.Services.Payment;
using BareeraBangles.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace BareeraBangles.Controllers;

public class CheckoutController : Controller
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;
    private readonly IPaymentOrchestrator _paymentOrchestrator;
    private readonly ICouponService _couponService;
    private readonly IEmailService _emailService;
    private readonly JazzCashPaymentService _jazzCash;
    private readonly CardPaymentService _cardPayment;
    private readonly StoreSettings _storeSettings;
    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(
        ICartService cartService,
        IOrderService orderService,
        IPaymentOrchestrator paymentOrchestrator,
        ICouponService couponService,
        IEmailService emailService,
        JazzCashPaymentService jazzCash,
        CardPaymentService cardPayment,
        IOptions<StoreSettings> storeSettings,
        ILogger<CheckoutController> logger)
    {
        _cartService = cartService;
        _orderService = orderService;
        _paymentOrchestrator = paymentOrchestrator;
        _couponService = couponService;
        _emailService = emailService;
        _jazzCash = jazzCash;
        _cardPayment = cardPayment;
        _storeSettings = storeSettings.Value;
        _logger = logger;
    }

    private async Task<CheckoutViewModel> BuildCheckoutModelAsync(CheckoutViewModel? incoming = null)
    {
        var cart = await _cartService.GetCartAsync();
        var model = incoming ?? new CheckoutViewModel();
        model.Cart = cart;
        model.FreeShippingThreshold = _storeSettings.FreeShippingThreshold;

        var (_, _, discount, freeShip) = await _couponService.ValidateAsync(model.CouponCode, cart.GrandTotal);
        model.DiscountAmount = discount;

        var afterDiscount = Math.Max(0, cart.GrandTotal - discount);
        model.ShippingFee = freeShip || afterDiscount >= _storeSettings.FreeShippingThreshold
            ? 0
            : _storeSettings.FlatShippingFee;

        return model;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cart = await _cartService.GetCartAsync();
        if (!cart.Items.Any())
        {
            TempData["Error"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }

        var model = await BuildCheckoutModelAsync();
        ViewData["Title"] = "Checkout";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyCoupon(CheckoutViewModel model)
    {
        model = await BuildCheckoutModelAsync(model);
        if (!model.Cart.Items.Any())
        {
            return RedirectToAction("Index", "Cart");
        }

        var (ok, message, _, _) = await _couponService.ValidateAsync(model.CouponCode, model.Cart.GrandTotal);
        TempData[ok ? "Success" : "Error"] = string.IsNullOrEmpty(message)
            ? (ok ? "Totals updated." : "Could not apply coupon.")
            : message;

        ViewData["Title"] = "Checkout";
        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CheckoutViewModel model)
    {
        model = await BuildCheckoutModelAsync(model);
        if (!model.Cart.Items.Any())
        {
            ModelState.AddModelError(string.Empty, "Your cart is empty.");
        }

        var (couponOk, couponMessage, _, _) = await _couponService.ValidateAsync(model.CouponCode, model.Cart.GrandTotal);
        if (!couponOk)
        {
            ModelState.AddModelError(nameof(model.CouponCode), couponMessage);
        }

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Checkout";
            return View(model);
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _orderService.CreateOrderAsync(model, userId);
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var paymentResult = await _paymentOrchestrator.InitiateAsync(order, baseUrl);

            await SendOrderEmailAsync(order);

            if (paymentResult.IsReady && !string.IsNullOrEmpty(paymentResult.RedirectUrl) && paymentResult.FormFields.Any())
            {
                TempData["JazzCashUrl"] = paymentResult.RedirectUrl;
                TempData["JazzCashFields"] = JsonSerializer.Serialize(paymentResult.FormFields);
                return RedirectToAction(nameof(JazzCashRedirect), new { orderNumber = order.OrderNumber });
            }

            if (paymentResult.IsReady && !string.IsNullOrEmpty(paymentResult.RedirectUrl))
            {
                return Redirect(paymentResult.RedirectUrl);
            }

            order.RequiresGatewayConfiguration = paymentResult.RequiresConfiguration;
            order.PaymentMessage = paymentResult.Message;
            TempData["PaymentMessage"] = paymentResult.Message;
            return RedirectToAction(nameof(Confirmation), new { orderNumber = order.OrderNumber });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewData["Title"] = "Checkout";
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> JazzCashRedirect(string orderNumber)
    {
        var order = await _orderService.GetByOrderNumberAsync(orderNumber);
        if (order is null) return NotFound();

        ViewBag.PaymentUrl = TempData["JazzCashUrl"]?.ToString();
        ViewBag.FieldsJson = TempData["JazzCashFields"]?.ToString() ?? "{}";
        ViewData["Title"] = "Redirecting to JazzCash";
        return View(order);
    }

    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> PaymentReturn(string? orderNumber, int? cancelled)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (Request.HasFormContentType)
        {
            foreach (var key in Request.Form.Keys)
            {
                fields[key] = Request.Form[key].ToString();
            }
        }
        foreach (var kv in Request.Query)
        {
            if (!fields.ContainsKey(kv.Key))
            {
                fields[kv.Key] = kv.Value.ToString();
            }
        }

        orderNumber ??= fields.GetValueOrDefault("pp_BillReference")
                        ?? fields.GetValueOrDefault("orderNumber");

        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            return NotFound();
        }

        var order = await _orderService.GetByOrderNumberAsync(orderNumber);
        if (order is null) return NotFound();

        if (cancelled == 1)
        {
            TempData["Error"] = "Payment was cancelled. Your order is still reserved as payment pending.";
            return RedirectToAction(nameof(Confirmation), new { orderNumber });
        }

        if (fields.ContainsKey("pp_ResponseCode") || fields.ContainsKey("pp_SecureHash"))
        {
            if (_jazzCash.TryValidateReturn(fields, out var jazzMessage))
            {
                await _orderService.MarkPaidFromGatewayAsync(orderNumber, fields.GetValueOrDefault("pp_TxnRefNo"), jazzMessage);
                TempData["Success"] = "Payment confirmed. Thank you!";
            }
            else
            {
                TempData["Error"] = jazzMessage;
            }
        }
        else
        {
            TempData["Success"] = "Returned from payment provider. Final status updates when the gateway confirms the transaction.";
        }

        return RedirectToAction(nameof(Confirmation), new { orderNumber });
    }

    [HttpPost("/Checkout/StripeWebhook")]
    public async Task<IActionResult> StripeWebhook()
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var payload = await reader.ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].ToString();

        if (!_cardPayment.VerifyWebhookSignature(payload, signature))
        {
            _logger.LogWarning("Stripe webhook signature validation failed.");
            return BadRequest();
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var type = doc.RootElement.GetProperty("type").GetString();
            if (type == "checkout.session.completed")
            {
                var data = doc.RootElement.GetProperty("data").GetProperty("object");
                var orderNumber = data.TryGetProperty("client_reference_id", out var cref)
                    ? cref.GetString()
                    : null;
                if (string.IsNullOrEmpty(orderNumber) &&
                    data.TryGetProperty("metadata", out var meta) &&
                    meta.TryGetProperty("orderNumber", out var on))
                {
                    orderNumber = on.GetString();
                }

                var sessionId = data.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                if (!string.IsNullOrEmpty(orderNumber))
                {
                    await _orderService.MarkPaidFromGatewayAsync(orderNumber, sessionId, "Stripe checkout.session.completed");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe webhook processing failed.");
            return BadRequest();
        }

        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(string orderNumber)
    {
        var order = await _orderService.GetByOrderNumberAsync(orderNumber);
        if (order is null) return NotFound();

        order.PaymentMessage ??= TempData["PaymentMessage"]?.ToString();
        ViewData["Title"] = "Order Confirmation";
        return View(order);
    }

    private async Task SendOrderEmailAsync(OrderViewModel order)
    {
        if (string.IsNullOrWhiteSpace(order.Email)) return;
        try
        {
            var body = $"<p>Hi {System.Net.WebUtility.HtmlEncode(order.FullName)},</p>" +
                       $"<p>Thanks for ordering from {_storeSettings.StoreName}.</p>" +
                       $"<p><strong>Order:</strong> {order.OrderNumber}<br/>" +
                       $"<strong>Total:</strong> {_storeSettings.CurrencySymbol} {order.GrandTotal:N0}<br/>" +
                       $"<strong>Payment:</strong> {order.PaymentMethod}<br/>" +
                       $"<strong>Status:</strong> {order.Status}</p>" +
                       "<p>Track your order anytime with your order number and phone.</p>";
            await _emailService.SendAsync(order.Email, $"Order {order.OrderNumber} received", body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not send order email for {Order}", order.OrderNumber);
        }
    }
}
