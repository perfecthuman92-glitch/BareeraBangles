namespace BareeraBangles.Configuration;

public class StoreSettings
{
    public const string SectionName = "StoreSettings";

    public string StoreName { get; set; } = "Bareera Bangles";
    public string WhatsAppNumber { get; set; } = "923001234567";
    public string Currency { get; set; } = "PKR";
    public string CurrencySymbol { get; set; } = "Rs.";
    public string Tagline { get; set; } = "Elegance on Every Wrist";
    public string SupportEmail { get; set; } = "hello@bareerabangles.com";
    public decimal FlatShippingFee { get; set; } = 250;
    public decimal FreeShippingThreshold { get; set; } = 5000;
    public int LowStockThreshold { get; set; } = 5;
    public bool ShowPromoBanner { get; set; } = true;
    public string PromoBannerText { get; set; } = "Free shipping on orders Rs. 5,000+ · Use code BAREERA10 for 10% off";
    public string PromoBannerLink { get; set; } = "/shop";
    public string InstagramUrl { get; set; } = "https://instagram.com/bareerabangles";
    public string FacebookUrl { get; set; } = "https://facebook.com/bareerabangles";
}

public class EmailSettings
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "noreply@bareerabangles.com";
    public string FromName { get; set; } = "Bareera Bangles";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SmtpHost) &&
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrWhiteSpace(Password);
}

public class JazzCashSettings
{
    public const string SectionName = "JazzCash";

    public string MerchantId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string IntegritySalt { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string PaymentUrl { get; set; } = "https://sandbox.jazzcash.com.pk/CustomerPortal/transactionmanagement/merchantform/";
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(MerchantId) &&
        !string.IsNullOrWhiteSpace(Password) &&
        !string.IsNullOrWhiteSpace(IntegritySalt);
}

public class CardPaymentSettings
{
    public const string SectionName = "CardPayment";

    public string Provider { get; set; } = "Stripe";
    public string PublishableKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SecretKey) &&
        !string.IsNullOrWhiteSpace(PublishableKey);
}
