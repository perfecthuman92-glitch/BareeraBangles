using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BareeraBangles.Configuration;
using BareeraBangles.Data;
using BareeraBangles.Models;
using BareeraBangles.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BareeraBangles.Services.Payment;

public class CardPaymentService : IPaymentService
{
    private readonly CardPaymentSettings _settings;
    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CardPaymentService> _logger;

    public CardPaymentService(
        IOptions<CardPaymentSettings> settings,
        ApplicationDbContext db,
        IHttpClientFactory httpClientFactory,
        ILogger<CardPaymentService> logger)
    {
        _settings = settings.Value;
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public PaymentMethod Method => PaymentMethod.BankCard;

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
            payment.ProviderMessage =
                $"Card payment provider ({_settings.Provider}) is not configured. " +
                "Set CardPayment:SecretKey and CardPayment:PublishableKey via User Secrets. " +
                "Card data never touches this server — hosted checkout only.";
            payment.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new PaymentInitiationResult
            {
                IsReady = false,
                RequiresConfiguration = true,
                Status = PaymentStatus.AwaitingProvider,
                Message = payment.ProviderMessage
            };
        }

        var successUrl = string.IsNullOrWhiteSpace(_settings.SuccessUrl)
            ? $"{returnBaseUrl.TrimEnd('/')}/Checkout/Confirmation?orderNumber={Uri.EscapeDataString(order.OrderNumber)}"
            : _settings.SuccessUrl;
        var cancelUrl = string.IsNullOrWhiteSpace(_settings.CancelUrl)
            ? $"{returnBaseUrl.TrimEnd('/')}/Checkout/PaymentReturn?orderNumber={Uri.EscapeDataString(order.OrderNumber)}&cancelled=1"
            : _settings.CancelUrl;

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.SecretKey);

            var amountPaisa = ((long)(order.GrandTotal * 100)).ToString(CultureInfo.InvariantCulture);
            var form = new Dictionary<string, string>
            {
                ["mode"] = "payment",
                ["success_url"] = successUrl + (successUrl.Contains('?') ? "&" : "?") + "session_id={CHECKOUT_SESSION_ID}",
                ["cancel_url"] = cancelUrl,
                ["client_reference_id"] = order.OrderNumber,
                ["line_items[0][price_data][currency]"] = "pkr",
                ["line_items[0][price_data][product_data][name]"] = $"Bareera Bangles order {order.OrderNumber}",
                ["line_items[0][price_data][unit_amount]"] = amountPaisa,
                ["line_items[0][quantity]"] = "1",
                ["metadata[orderNumber]"] = order.OrderNumber
            };

            using var content = new FormUrlEncodedContent(form);
            using var response = await client.PostAsync("https://api.stripe.com/v1/checkout/sessions", content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Stripe Checkout session failed: {Body}", body);
                payment.Status = PaymentStatus.Failed;
                payment.ProviderMessage = "Could not start card checkout. Please try again or choose another method.";
                payment.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return new PaymentInitiationResult
                {
                    IsReady = false,
                    Status = PaymentStatus.Failed,
                    Message = payment.ProviderMessage
                };
            }

            using var doc = JsonDocument.Parse(body);
            var sessionId = doc.RootElement.GetProperty("id").GetString();
            var url = doc.RootElement.GetProperty("url").GetString();

            payment.Status = PaymentStatus.AwaitingProvider;
            payment.ProviderReference = sessionId;
            payment.ProviderMessage = "Redirecting to Stripe hosted checkout.";
            payment.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new PaymentInitiationResult
            {
                IsReady = true,
                Status = PaymentStatus.AwaitingProvider,
                RedirectUrl = url,
                Message = "Complete payment on the secure card page."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe Checkout initiation failed for {Order}", order.OrderNumber);
            payment.Status = PaymentStatus.Failed;
            payment.ProviderMessage = "Card checkout could not be started.";
            payment.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return new PaymentInitiationResult
            {
                IsReady = false,
                Status = PaymentStatus.Failed,
                Message = payment.ProviderMessage
            };
        }
    }

    public bool VerifyWebhookSignature(string payload, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(_settings.WebhookSecret) || string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        // Stripe-Signature: t=timestamp,v1=signature
        string? timestamp = null;
        string? v1 = null;
        foreach (var part in signatureHeader.Split(','))
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2) continue;
            if (kv[0].Trim() == "t") timestamp = kv[1].Trim();
            if (kv[0].Trim() == "v1") v1 = kv[1].Trim();
        }

        if (timestamp is null || v1 is null) return false;

        var signed = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.WebhookSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signed));
        var expected = Convert.ToHexString(hash).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(v1.ToLowerInvariant()));
    }
}
