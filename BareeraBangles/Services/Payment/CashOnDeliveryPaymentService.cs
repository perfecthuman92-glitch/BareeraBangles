using BareeraBangles.Data;
using BareeraBangles.Models;
using BareeraBangles.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BareeraBangles.Services.Payment;

public class CashOnDeliveryPaymentService : IPaymentService
{
    private readonly ApplicationDbContext _db;

    public CashOnDeliveryPaymentService(ApplicationDbContext db)
    {
        _db = db;
    }

    public PaymentMethod Method => PaymentMethod.CashOnDelivery;

    public async Task<PaymentInitiationResult> InitiateAsync(OrderViewModel order, string returnBaseUrl)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.OrderId == order.Id);
        if (payment is not null)
        {
            payment.Status = PaymentStatus.Pending;
            payment.ProviderMessage = "Cash on delivery. Collect payment when the order is delivered.";
            payment.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return new PaymentInitiationResult
        {
            IsReady = false,
            RequiresConfiguration = false,
            Status = PaymentStatus.Pending,
            Message = "Order placed for cash on delivery. Pay the courier when your order arrives."
        };
    }
}
