using System.Globalization;
using System.Text;
using BareeraBangles.Configuration;
using BareeraBangles.ViewModels;
using Microsoft.Extensions.Options;

namespace BareeraBangles.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly StoreSettings _settings;

    public WhatsAppService(IOptions<StoreSettings> settings)
    {
        _settings = settings.Value;
    }

    public string GetDisplayNumber() => _settings.WhatsAppNumber;

    public string BuildProductOrderUrl(string productName, string? size, int quantity, decimal price)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Hello, I am interested in ordering from {_settings.StoreName}:");
        sb.AppendLine($"Product: {productName}");
        if (!string.IsNullOrWhiteSpace(size))
        {
            sb.AppendLine($"Size: {size}");
        }
        sb.AppendLine($"Quantity: {quantity}");
        sb.AppendLine($"Price: {_settings.CurrencySymbol} {price.ToString("N0", CultureInfo.InvariantCulture)}");
        return BuildUrl(sb.ToString());
    }

    public string BuildCartOrderUrl(CartViewModel cart)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Hello, I would like to place an order from {_settings.StoreName}:");
        foreach (var item in cart.Items)
        {
            sb.AppendLine($"- {item.ProductName} | Size: {item.SelectedSize} | Qty: {item.Quantity} | {_settings.CurrencySymbol} {item.Subtotal:N0}");
        }
        sb.AppendLine($"Total: {_settings.CurrencySymbol} {cart.GrandTotal:N0}");
        return BuildUrl(sb.ToString());
    }

    public string BuildGeneralChatUrl(string? message = null)
    {
        var text = string.IsNullOrWhiteSpace(message)
            ? $"Hello! I would like to know more about {_settings.StoreName}."
            : message;
        return BuildUrl(text);
    }

    private string BuildUrl(string message)
    {
        var number = new string((_settings.WhatsAppNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        var encoded = Uri.EscapeDataString(message.Trim());
        return $"https://wa.me/{number}?text={encoded}";
    }
}
