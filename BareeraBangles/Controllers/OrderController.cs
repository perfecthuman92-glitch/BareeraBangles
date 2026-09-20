using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BareeraBangles.Services;
using BareeraBangles.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BareeraBangles.Controllers;

public class OrderController : Controller
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> MyOrders()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var orders = await _orderService.GetForUserAsync(userId);
        ViewData["Title"] = "My Orders";
        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Details(string orderNumber, string? phone)
    {
        OrderViewModel? order = null;
        if (User.IsInRole("Admin"))
        {
            order = await _orderService.GetByOrderNumberAsync(orderNumber);
        }
        else if (!string.IsNullOrWhiteSpace(phone))
        {
            order = await _orderService.FindByOrderNumberAndPhoneAsync(orderNumber, phone);
        }
        else if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var mine = await _orderService.GetForUserAsync(userId);
            order = mine.FirstOrDefault(o => o.OrderNumber == orderNumber);
        }

        if (order is null)
        {
            TempData["Error"] = "Provide your phone number with the order, or sign in to view your orders.";
            return RedirectToAction(nameof(Track));
        }

        ViewData["Title"] = $"Order {order.OrderNumber}";
        return View(order);
    }

    [HttpGet]
    public async Task<IActionResult> Receipt(string orderNumber, string? phone)
    {
        OrderViewModel? order = null;
        if (!string.IsNullOrWhiteSpace(phone))
        {
            order = await _orderService.FindByOrderNumberAndPhoneAsync(orderNumber, phone);
        }
        else if (User.IsInRole("Admin"))
        {
            order = await _orderService.GetByOrderNumberAsync(orderNumber);
        }
        else if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var mine = await _orderService.GetForUserAsync(userId);
            order = mine.FirstOrDefault(o => o.OrderNumber == orderNumber);
        }

        if (order is null) return NotFound();
        ViewData["Title"] = $"Receipt {order.OrderNumber}";
        return View(order);
    }

    [HttpGet]
    public IActionResult Track()
    {
        ViewData["Title"] = "Track Order";
        return View(new TrackOrderViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Track(TrackOrderViewModel model)
    {
        ViewData["Title"] = "Track Order";
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var order = await _orderService.FindByOrderNumberAndPhoneAsync(model.OrderNumber, model.PhoneNumber);
        if (order is null)
        {
            ModelState.AddModelError(string.Empty, "No order found for that order number and phone combination.");
            return View(model);
        }

        model.Order = order;
        return View(model);
    }
}

public class TrackOrderViewModel
{
    [Required, Display(Name = "Order Number")]
    public string OrderNumber { get; set; } = string.Empty;

    [Required, Display(Name = "Phone Number"), Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    public OrderViewModel? Order { get; set; }
}
