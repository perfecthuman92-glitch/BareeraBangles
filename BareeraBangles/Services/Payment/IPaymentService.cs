using BareeraBangles.Models;
using BareeraBangles.ViewModels;

namespace BareeraBangles.Services.Payment;

public class PaymentInitiationResult
{
    public bool IsReady { get; set; }
    public bool RequiresConfiguration { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
    public Dictionary<string, string> FormFields { get; set; } = new();
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
}

public interface IPaymentService
{
    PaymentMethod Method { get; }
    Task<PaymentInitiationResult> InitiateAsync(OrderViewModel order, string returnBaseUrl);
}

public interface IPaymentOrchestrator
{
    Task<PaymentInitiationResult> InitiateAsync(OrderViewModel order, string returnBaseUrl);
}
