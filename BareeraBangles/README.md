# Bareera Bangles — ASP.NET Core MVC Retail Store

Production-style bangle e-commerce site built with ASP.NET Core MVC, EF Core, Identity, and Razor views.

## Features

- Shop with search, category / price / size / age-group filters and sorting
- Adult sizes (2.2–2.8) and configurable baby/kids size labels
- Size required before add-to-cart
- Cart, checkout, order confirmation
- JazzCash + bank-card payment **integration points** (no fake success; credentials via secrets)
- WhatsApp ordering (configurable number)
- Admin area: products, sizes, categories, orders
- Seeded demo catalog (12 products)

## Prerequisites

- .NET 8 SDK
- Default DB: **SQLite** (no install needed)
- Optional: SQL Server / LocalDB for production-style hosting

## Configure

Edit `appsettings.json` or use User Secrets:

```bash
cd BareeraBangles
dotnet user-secrets set "StoreSettings:WhatsAppNumber" "923001234567"
dotnet user-secrets set "JazzCash:MerchantId" "YOUR_MERCHANT_ID"
dotnet user-secrets set "JazzCash:Password" "YOUR_PASSWORD"
dotnet user-secrets set "JazzCash:IntegritySalt" "YOUR_SALT"
dotnet user-secrets set "CardPayment:PublishableKey" "pk_test_..."
dotnet user-secrets set "CardPayment:SecretKey" "sk_test_..."
```

### Database

**SQLite (default):**

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=bareera_bangles.db"
}
```

**SQL Server:**

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BareeraBangles;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

The app picks SQLite vs SQL Server from the connection string automatically.

## Run

```bash
cd D:\Bareera_Bangles\BareeraBangles
dotnet restore
dotnet ef database update
dotnet run
```

On first startup the app also runs migrations + seed data automatically.

Open the HTTPS URL shown in the console (typically `https://localhost:7xxx`).

## Admin login (seeded)

- URL: `/Admin`
- Email: `admin@bareerabangles.com`
- Password: `Admin@12345`

## Payment notes

- JazzCash: when credentials are set, checkout posts to the JazzCash hosted form.
- Bank card: `CardPaymentService` is the Stripe/hosted-checkout integration point. Raw card data is never stored.
- If credentials are missing, the order is still created as **Payment Pending** with a clear configuration message — no fake paid status.
