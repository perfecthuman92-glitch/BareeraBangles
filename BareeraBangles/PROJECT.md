# Bareera Bangles — Project Documentation

ASP.NET Core MVC retail store for adult and baby/kids bangles, built from `Bangle_Retail_MVC_Cursor_Prompt.pdf` and extended in Parts 2–3.

---

## Quick start

```bash
cd D:\Bareera_Bangles\BareeraBangles
dotnet restore
dotnet ef database update
dotnet run --urls "http://localhost:5188"
```

Open **http://localhost:5188**

| Role | Credentials |
|------|-------------|
| Admin | `admin@bareerabangles.com` / `Admin@12345` |
| Admin URL | `/Admin` |

Also see [SETUP.md](SETUP.md) for SQL Server, WhatsApp, JazzCash, and card payment configuration.

---

## What’s included

### Part 1 — Core store (PDF scope)

- Home page (hero, featured products, categories, why us, promo, testimonials, gallery, footer)
- Shop with search, category / price / size / age-group filters, sort, pagination
- Product details: gallery, adult + baby/kids sizes, quantity, Add to Cart, Buy Now, WhatsApp order
- Size required before add-to-cart
- Cart, checkout, order confirmation
- JazzCash + bank-card **payment integration points** (no fake paid status; secrets via config)
- Configurable WhatsApp number and prefilled messages
- Admin: products, sizes, images, categories, orders + status updates
- Identity auth, seed data (12 products, 7 categories)
- EF Core + SQLite by default (SQL Server supported via connection string)

### Part 2 — Customer UX

- Size Guide page (`/Home/SizeGuide`)
- Track Order by order number + phone (`/Order/Track`)
- Branded Login / Register
- Floating WhatsApp button
- Admin size templates (adult 2.2–2.8, baby/kids age labels)

### Part 3 — Retail extras

- Wishlist (♥ on cards, `/Wishlist`)
- Flat shipping fee on checkout (default Rs. 250)
- Printable order receipt
- Admin dashboard: recent orders + low-stock alerts
- Cancelling an order restores size stock

### Part 4 — Growth features

- Recently viewed products (home + product pages)
- Coupon codes at checkout (`BAREERA10`, `WELCOME500`)
- Free shipping above threshold (default Rs. 5,000 after discount)
- Newsletter signup in footer + admin subscriber list
- Admin Coupons management (`/Admin/Coupons`)

### Part 5 — Reviews, search & ops

- Product reviews (submit on product page; admin approve/reject)
- Header search with autocomplete suggestions
- Gift order + gift message at checkout
- Admin orders CSV export
- Admin Reviews queue (`/Admin/Reviews`)

### Part 6 — Content, SEO & ops insights

- FAQ page (`/Home/Faq`)
- Configurable home promo banner (`StoreSettings:ShowPromoBanner`, `PromoBannerText`)
- Admin analytics: paid revenue, 30-day revenue, top products, subscriber/review/alert counts
- Back-in-stock email alerts on product pages + Admin → Stock Alerts
- `robots.txt` and `sitemap.xml`

### Part 7 — Compare, share & polish

- Compare up to 3 products (`/Compare`)
- Share product (copy link + WhatsApp)
- Cookie consent banner
- Health endpoint (`/health`)
- Admin internal order notes

### Part 8 — Go-live completion

- Cash on delivery checkout option
- JazzCash return verification (marks Paid when gateway confirms)
- Stripe Checkout session + webhook endpoint when keys are set
- Order confirmation + stock-alert + contact emails (SMTP or `App_Data/mail` fallback)
- My Orders for signed-in customers
- FREESHIP coupon + coupon activate/deactivate
- Contact form, newsletter admin nav, approved reviews on home

---

## Project structure

```text
BareeraBangles/
├── Areas/
│   ├── Admin/          # Dashboard, Products, Categories, Orders
│   └── Identity/       # Login, Register
├── Configuration/      # StoreSettings, JazzCash, CardPayment
├── Controllers/        # Home, Product, Cart, Checkout, Order, WhatsApp, Wishlist
├── Data/               # ApplicationDbContext, DbSeeder, Migrations
├── Models/             # Product, ProductSize, Order, Cart, Payment, …
├── Services/           # Product, Cart, Order, WhatsApp, Wishlist, Payment/
├── ViewModels/
├── Views/
├── wwwroot/            # css, js, images, uploads
├── appsettings.json
├── Program.cs
├── README.md
├── SETUP.md
└── PROJECT.md          # this file
```

---

## Main routes

| URL | Description |
|-----|-------------|
| `/` | Home |
| `/shop` | Product listing |
| `/product/{slug}` | Product details |
| `/product/quick-view/{id}` | Quick view partial |
| `/Cart` | Shopping cart |
| `/Checkout` | Checkout |
| `/Checkout/Confirmation` | Order confirmation |
| `/Wishlist` | Saved products |
| `/Order/Track` | Track order |
| `/Order/Receipt` | Printable receipt |
| `/Home/SizeGuide` | Size guide |
| `/WhatsApp/Chat` | Opens WhatsApp |
| `/Admin` | Admin dashboard |
| `/Identity/Account/Login` | Login |

---

## Configuration (`appsettings.json`)

```json
"StoreSettings": {
  "StoreName": "Bareera Bangles",
  "WhatsAppNumber": "923001234567",
  "Currency": "PKR",
  "CurrencySymbol": "Rs.",
  "FlatShippingFee": 250,
  "LowStockThreshold": 5
}
```

**Database**

- Default: `"Data Source=bareera_bangles.db"` (SQLite)
- SQL Server: use a `Server=...` connection string (see SETUP.md)

**Payments** — use user secrets, never commit credentials:

```bash
dotnet user-secrets set "JazzCash:MerchantId" "..."
dotnet user-secrets set "JazzCash:Password" "..."
dotnet user-secrets set "JazzCash:IntegritySalt" "..."
dotnet user-secrets set "CardPayment:PublishableKey" "..."
dotnet user-secrets set "CardPayment:SecretKey" "..."
```

---

## Sizing model

- **Adult:** labels such as `2.2`, `2.4`, `2.6`, `2.8` with optional measurement notes
- **Baby/Kids:** age labels (Newborn, 0–6 Months, …) — configurable per product in admin
- **Unisex:** can combine both
- Customers must select a size before adding to cart; selected size is stored on cart and order items

---

## Order & payment flow

1. Customer selects size → cart → checkout  
2. Order created as **Payment Pending**; stock reduced  
3. JazzCash / card service initiates gateway redirect **only if credentials are configured**  
4. If not configured, confirmation shows a clear message (no fake “Paid”)  
5. Admin can update status (Pending → Paid → Processing → Shipped → Delivered / Cancelled)  
6. **Cancelled** restores stock for matching product sizes  

---

## Tech stack

- ASP.NET Core 8 MVC + Razor Views  
- Entity Framework Core  
- ASP.NET Core Identity  
- SQLite (dev default) / SQL Server  
- Bootstrap 5 + custom CSS  
- Session cart & wishlist  

---

## Useful commands

```bash
dotnet build
dotnet run
dotnet ef migrations add MigrationName --output-dir Data/Migrations
dotnet ef database update
```

---

## Related files

- [README.md](README.md) — short overview  
- [SETUP.md](SETUP.md) — run, DB, WhatsApp, payment setup checklist  
- `../Bangle_Retail_MVC_Cursor_Prompt.pdf` — original build prompt  
