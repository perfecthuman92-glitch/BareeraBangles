using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BareeraBangles.Configuration;
using BareeraBangles.Data;
using BareeraBangles.Models;
using BareeraBangles.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BareeraBangles.Services.Payment;

public class JazzCashPaymentService : IPaymentService
{
    private readonly JazzCashSettings _settings;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<JazzCashPaymentService> _logger;

    public JazzCashPaymentService(
        IOptions<JazzCashSettings> settings,
        ApplicationDbContext db,
        ILogger<JazzCashPaymentService> logger)
    {
        _settings = settings.Value;
        _db = db;
        _logger = logger;
    }

    public PaymentMethod Method => PaymentMethod.JazzCash;

    public async Task<PaymentInitiationResult> InitiateAsync(OrderViewModel order, string returnBaseUrl)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.OrderId == order.Id);
        if (payment is null)
        {
            return new PaymentInitiationResult
            {
                IsReady = false,
                Message = "Payment record not found for this order."
            };
        }

        if (!_settings.IsConfigured)
        {
            payment.Status = PaymentStatus.AwaitingProvider;
            payment.ProviderMessage = "JazzCash credentials are not configured. Set MerchantId, Password, and IntegritySalt via User Secrets or environment variables.";
            payment.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogWarning("JazzCash payment initiated for {OrderNumber} but credentials are not configured.", order.OrderNumber);

            return new PaymentInitiationResult
            {
                IsReady = false,
                RequiresConfiguration = true,
                Status = PaymentStatus.AwaitingProvider,
                Message = payment.ProviderMessage
            };
        }

        var amount = ((int)(order.GrandTotal * 100)).ToString(CultureInfo.InvariantCulture);
        var txnRef = $"{DateTime.Now:yyyyMMddHHmmss}{order.Id}";
        var returnUrl = string.IsNullOrWhiteSpace(_settings.ReturnUrl)
            ? $"{returnBaseUrl.TrimEnd('/')}/Checkout/PaymentReturn?orderNumber={Uri.EscapeDataString(order.OrderNumber)}"
            : _settings.ReturnUrl;

        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["pp_Version"] = "1.1",
            ["pp_TxnType"] = "MWALLET",
            ["pp_Language"] = "EN",
            ["pp_MerchantID"] = _settings.MerchantId,
            ["pp_Password"] = _settings.Password,
            ["pp_TxnRefNo"] = txnRef,
            ["pp_Amount"] = amount,
            ["pp_TxnCurrency"] = "PKR",
            ["pp_TxnDateTime"] = DateTime.Now.ToString("yyyyMMddHHmmss"),
            ["pp_BillReference"] = order.OrderNumber,
            ["pp_Description"] = $"Bareera Bangles order {order.OrderNumber}",
            ["pp_ReturnURL"] = returnUrl,
            ["pp_TxnExpiryDateTime"] = DateTime.Now.AddHours(2).ToString("yyyyMMddHHmmss")
        };

        fields["pp_SecureHash"] = ComputeSecureHash(fields, _settings.IntegritySalt);

        payment.Status = PaymentStatus.AwaitingProvider;
        payment.ProviderReference = txnRef;
        payment.ProviderMessage = "Redirecting customer to JazzCash hosted payment form.";
        payment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new PaymentInitiationResult
        {
            IsReady = true,
            Status = PaymentStatus.AwaitingProvider,
            RedirectUrl = _settings.PaymentUrl,
            FormFields = new Dictionary<string, string>(fields),
            Message = "Complete payment on the JazzCash secure page."
        };
    }

    public bool TryValidateReturn(IReadOnlyDictionary<string, string> fields, out string message)
    {
        message = string.Empty;
        if (!_settings.IsConfigured)
        {
            message = "JazzCash is not configured.";
            return false;
        }

        if (!fields.TryGetValue("pp_ResponseCode", out var code))
        {
            message = "Missing JazzCash response code.";
            return false;
        }

        if (fields.TryGetValue("pp_SecureHash", out var receivedHash) && !string.IsNullOrEmpty(receivedHash))
        {
            var sorted = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var kv in fields.Where(k =>
                         !string.Equals(k.Key, "pp_SecureHash", StringComparison.OrdinalIgnoreCase) &&
                         !string.IsNullOrEmpty(k.Value)))
            {
                sorted[kv.Key] = kv.Value;
            }

            var expected = ComputeSecureHash(sorted, _settings.IntegritySalt);
            if (!string.Equals(expected, receivedHash, StringComparison.OrdinalIgnoreCase))
            {
                message = "JazzCash secure hash validation failed.";
                _logger.LogWarning("JazzCash hash mismatch for bill {Bill}", fields.GetValueOrDefault("pp_BillReference"));
                return false;
            }
        }

        if (code is "000" or "121")
        {
            message = "JazzCash payment confirmed.";
            return true;
        }

        message = fields.GetValueOrDefault("pp_ResponseMessage") ?? $"JazzCash response code {code}";
        return false;
    }

    private static string ComputeSecureHash(SortedDictionary<string, string> fields, string integritySalt)
    {
        var values = string.Join("&", fields.Where(kv => !string.IsNullOrEmpty(kv.Value)).Select(kv => kv.Value));
        var payload = $"{integritySalt}&{values}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(integritySalt));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }
}

public class PaymentOrchestrator : IPaymentOrchestrator
{
    private readonly IEnumerable<IPaymentService> _services;

    public PaymentOrchestrator(IEnumerable<IPaymentService> services)
    {
        _services = services;
    }

    public Task<PaymentInitiationResult> InitiateAsync(OrderViewModel order, string returnBaseUrl)
    {
        var service = _services.FirstOrDefault(s => s.Method == order.PaymentMethod)
                      ?? throw new InvalidOperationException($"No payment service registered for {order.PaymentMethod}.");
        return service.InitiateAsync(order, returnBaseUrl);
    }
}
