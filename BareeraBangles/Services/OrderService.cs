using BareeraBangles.Configuration;
using BareeraBangles.Data;
using BareeraBangles.Models;
using BareeraBangles.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BareeraBangles.Services;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _db;
    private readonly ICartService _cartService;
    private readonly ICouponService _couponService;
    private readonly StoreSettings _storeSettings;

    public OrderService(
        ApplicationDbContext db,
        ICartService cartService,
        ICouponService couponService,
        IOptions<StoreSettings> storeSettings)
    {
        _db = db;
        _cartService = cartService;
        _couponService = couponService;
        _storeSettings = storeSettings.Value;
    }

    public async Task<OrderViewModel> CreateOrderAsync(CheckoutViewModel model, string? userId = null)
    {
        var cart = await _cartService.GetCartAsync();
        if (!cart.Items.Any())
        {
            throw new InvalidOperationException("Your cart is empty.");
        }

        foreach (var item in cart.Items)
        {
            var size = await _db.ProductSizes.FirstOrDefaultAsync(s => s.Id == item.ProductSizeId);
            if (size is null || !size.IsActive || size.StockQuantity < item.Quantity)
            {
                throw new InvalidOperationException($"Insufficient stock for {item.ProductName} size {item.SelectedSize}.");
            }
        }

        var (couponOk, couponMessage, discount, freeShip) = await _couponService.ValidateAsync(model.CouponCode, cart.GrandTotal);
        if (!couponOk)
        {
            throw new InvalidOperationException(couponMessage);
        }

        var discountedSubtotal = Math.Max(0, cart.GrandTotal - discount);
        var shipping = freeShip || discountedSubtotal >= _storeSettings.FreeShippingThreshold
            ? 0
            : _storeSettings.FlatShippingFee;

        var customer = await _db.Customers.FirstOrDefaultAsync(c =>
            (userId != null && c.UserId == userId) || c.PhoneNumber == model.PhoneNumber);
        if (customer is null)
        {
            customer = new Customer
            {
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                UserId = userId
            };
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();
        }
        else
        {
            customer.FullName = model.FullName;
            customer.Email = model.Email ?? customer.Email;
            if (userId is not null) customer.UserId = userId;
        }

        _db.Addresses.Add(new Address
        {
            CustomerId = customer.Id,
            ShippingAddress = model.ShippingAddress,
            City = model.City,
            PostalCode = model.PostalCode
        });

        var order = new Order
        {
            OrderNumber = GenerateOrderNumber(),
            CustomerId = customer.Id,
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber,
            Email = model.Email,
            ShippingAddress = model.ShippingAddress,
            City = model.City,
            PostalCode = model.PostalCode,
            OrderNotes = model.OrderNotes,
            IsGift = model.IsGift,
            GiftMessage = model.IsGift ? model.GiftMessage : null,
            PaymentMethod = model.PaymentMethod,
            Status = OrderStatus.PaymentPending,
            Subtotal = cart.GrandTotal,
            DiscountAmount = discount,
            CouponCode = string.IsNullOrWhiteSpace(model.CouponCode) ? null : model.CouponCode.Trim().ToUpperInvariant(),
            ShippingFee = shipping,
            GrandTotal = discountedSubtotal + shipping,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var item in cart.Items)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                SelectedSize = item.SelectedSize,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.Subtotal,
                ProductImageUrl = item.ImageUrl
            });

            var size = await _db.ProductSizes.FirstAsync(s => s.Id == item.ProductSizeId);
            size.StockQuantity -= item.Quantity;
        }

        order.Payment = new Models.Payment
        {
            Method = model.PaymentMethod,
            Status = PaymentStatus.Pending,
            Amount = order.GrandTotal,
            Currency = "PKR",
            CreatedAt = DateTime.UtcNow
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(order.CouponCode))
        {
            await _couponService.IncrementRedemptionAsync(order.CouponCode);
        }

        await _cartService.ClearAsync();
        return MapOrder(order);
    }

    public async Task<OrderViewModel?> GetByOrderNumberAsync(string orderNumber)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

        return order is null ? null : MapOrder(order);
    }

    public async Task<OrderViewModel?> FindByOrderNumberAndPhoneAsync(string orderNumber, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber) || string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        var normalizedPhone = new string(phoneNumber.Where(char.IsDigit).ToArray());
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber.Trim());

        if (order is null) return null;

        var orderPhone = new string(order.PhoneNumber.Where(char.IsDigit).ToArray());
        if (orderPhone != normalizedPhone && !orderPhone.EndsWith(normalizedPhone) && !normalizedPhone.EndsWith(orderPhone))
        {
            return null;
        }

        return MapOrder(order);
    }

    public async Task<List<OrderViewModel>> GetForUserAsync(string userId)
    {
        var customerIds = await _db.Customers
            .Where(c => c.UserId == userId)
            .Select(c => c.Id)
            .ToListAsync();

        var orders = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .Where(o => o.CustomerId.HasValue && customerIds.Contains(o.CustomerId.Value))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapOrder).ToList();
    }

    public async Task MarkPaidFromGatewayAsync(string orderNumber, string? providerReference, string? message)
    {
        var order = await _db.Orders
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
        if (order is null) return;
        if (order.Status is OrderStatus.Paid or OrderStatus.Processing or OrderStatus.Shipped or OrderStatus.Delivered)
        {
            return;
        }

        order.Status = OrderStatus.Paid;
        order.UpdatedAt = DateTime.UtcNow;
        if (order.Payment is not null)
        {
            order.Payment.Status = PaymentStatus.Completed;
            order.Payment.ProviderReference = providerReference ?? order.Payment.ProviderReference;
            order.Payment.ProviderMessage = message ?? "Payment confirmed by gateway.";
            order.Payment.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<Order>> GetAllOrdersAsync()
    {
        return await _db.Orders
            .AsNoTracking()
            .Include(o => o.Payment)
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<Order?> GetOrderEntityAsync(int id)
    {
        return await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task UpdateStatusAsync(int orderId, OrderStatus status)
    {
        var order = await _db.Orders
            .Include(o => o.Payment)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null) return;

        var previous = order.Status;
        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        if (status == OrderStatus.Paid && order.Payment is not null)
        {
            order.Payment.Status = PaymentStatus.Completed;
            order.Payment.UpdatedAt = DateTime.UtcNow;
        }

        if (status == OrderStatus.Cancelled && previous != OrderStatus.Cancelled)
        {
            if (order.Payment is not null && order.Payment.Status != PaymentStatus.Completed)
            {
                order.Payment.Status = PaymentStatus.Cancelled;
                order.Payment.UpdatedAt = DateTime.UtcNow;
            }

            foreach (var item in order.Items)
            {
                var size = await _db.ProductSizes.FirstOrDefaultAsync(s =>
                    s.ProductId == item.ProductId && s.Size == item.SelectedSize);
                if (size is not null)
                {
                    size.StockQuantity += item.Quantity;
                }
            }
        }

        await _db.SaveChangesAsync();
    }

    private static string GenerateOrderNumber()
    {
        return $"BB-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
    }

    public static OrderViewModel MapOrder(Order order)
    {
        return new OrderViewModel
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            PaymentMethod = order.PaymentMethod,
            PaymentStatus = order.Payment?.Status,
            FullName = order.FullName,
            PhoneNumber = order.PhoneNumber,
            Email = order.Email,
            ShippingAddress = order.ShippingAddress,
            City = order.City,
            PostalCode = order.PostalCode,
            OrderNotes = order.OrderNotes,
            IsGift = order.IsGift,
            GiftMessage = order.GiftMessage,
            Subtotal = order.Subtotal,
            ShippingFee = order.ShippingFee,
            DiscountAmount = order.DiscountAmount,
            CouponCode = order.CouponCode,
            GrandTotal = order.GrandTotal,
            CreatedAt = order.CreatedAt,
            PaymentMessage = order.Payment?.ProviderMessage,
            Items = order.Items.Select(i => new OrderItemViewModel
            {
                ProductName = i.ProductName,
                SelectedSize = i.SelectedSize,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice,
                ProductImageUrl = i.ProductImageUrl
            }).ToList()
        };
    }
}
