# Bareera Bangles — Run & Configure

## Run commands

```bash
cd D:\Bareera_Bangles\BareeraBangles
dotnet restore
dotnet ef database update
dotnet run
```

Or with a fixed URL:

```bash
dotnet run --urls "http://localhost:5188"
```

Then open the URL shown in the console (e.g. `http://localhost:5188`).

### Seeded admin

| Field | Value |
|-------|--------|
| URL | `/Admin` |
| Email | `admin@bareerabangles.com` |
| Password | `Admin@12345` |

---

## 1. Database (SQLite default / SQL Server)

**SQLite (works with no install — current default):**

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=bareera_bangles.db"
}
```

**SQL Server / LocalDB:**

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BareeraBangles;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

Or a full SQL Server instance:

```text
Server=YOUR_SERVER;Database=BareeraBangles;User Id=...;Password=...;TrustServerCertificate=True
```

The app chooses SQLite vs SQL Server from the connection string automatically. After changing it, run:

```bash
dotnet ef database update
```

> Note: migrations in this repo were generated for SQLite. For a fresh SQL Server database, remove `Data/Migrations`, switch the connection string, then run `dotnet ef migrations add InitialCreate` and `dotnet ef database update`.

---

## 2. WhatsApp number

In `appsettings.json`:

```json
"StoreSettings": {
  "StoreName": "Bareera Bangles",
  "WhatsAppNumber": "923001234567",
  "Currency": "PKR",
  "CurrencySymbol": "Rs."
}
```

Or via user secrets (recommended):

```bash
dotnet user-secrets set "StoreSettings:WhatsAppNumber" "923XXXXXXXXX"
```

Use country code without `+` or spaces (e.g. `923001234567`).

---

## 3. JazzCash credentials

Do **not** put secrets in source control. Use user secrets or environment variables:

```bash
dotnet user-secrets set "JazzCash:MerchantId" "YOUR_MERCHANT_ID"
dotnet user-secrets set "JazzCash:Password" "YOUR_PASSWORD"
dotnet user-secrets set "JazzCash:IntegritySalt" "YOUR_INTEGRITY_SALT"
dotnet user-secrets set "JazzCash:ReturnUrl" "https://your-domain/Checkout/PaymentReturn"
```

Optional: `JazzCash:PaymentUrl` (defaults to JazzCash sandbox merchant form).

When credentials are set, checkout posts the customer to JazzCash’s hosted form. When missing, the order is still created as **Payment Pending** with a clear message — never a fake “Paid” status.

---

## 4. Bank card provider (e.g. Stripe)

```bash
dotnet user-secrets set "CardPayment:Provider" "Stripe"
dotnet user-secrets set "CardPayment:PublishableKey" "pk_test_..."
dotnet user-secrets set "CardPayment:SecretKey" "sk_test_..."
dotnet user-secrets set "CardPayment:WebhookSecret" "whsec_..."
```

Card numbers and CVV are never collected or stored. Wire the provider SDK inside `Services/Payment/CardPaymentService.cs` to create a hosted Checkout Session, then redirect. Confirm payments via webhook.

---

## Verified checklist

- [x] Project builds
- [x] EF migrations + database create
- [x] Navigation (Home, Shop, About, Contact, Privacy, Terms)
- [x] Adult size selection (2.2–2.8)
- [x] Baby/kids size selection (Newborn, age bands)
- [x] Size required before add-to-cart
- [x] Cart stores selected size
- [x] Checkout validation
- [x] Order confirmation without fake payment success
- [x] WhatsApp links (`wa.me` with prefilled text)
- [x] Admin area protected by login
