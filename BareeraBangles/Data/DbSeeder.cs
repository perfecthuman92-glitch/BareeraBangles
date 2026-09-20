using BareeraBangles.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BareeraBangles.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        await context.Database.MigrateAsync();

        const string adminRole = "Admin";
        if (!await roleManager.RoleExistsAsync(adminRole))
        {
            await roleManager.CreateAsync(new IdentityRole(adminRole));
        }

        const string adminEmail = "admin@bareerabangles.com";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, "Admin@12345");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, adminRole);
            }
            else
            {
                logger.LogWarning("Admin user creation failed: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else if (!await userManager.IsInRoleAsync(admin, adminRole))
        {
            await userManager.AddToRoleAsync(admin, adminRole);
        }

        if (await context.Categories.AnyAsync())
        {
            await SeedCouponsIfEmptyAsync(context, logger);
            return;
        }

        var categories = new List<Category>
        {
            new() { Name = "Gold Bangles", Slug = "gold-bangles", Description = "Radiant gold-tone classics", DisplayOrder = 1, ImageUrl = "/images/categories/gold.svg" },
            new() { Name = "Bridal Bangles", Slug = "bridal-bangles", Description = "Statement sets for your special day", DisplayOrder = 2, ImageUrl = "/images/categories/bridal.svg" },
            new() { Name = "Traditional Bangles", Slug = "traditional-bangles", Description = "Heritage-inspired craftsmanship", DisplayOrder = 3, ImageUrl = "/images/categories/traditional.svg" },
            new() { Name = "Stone Bangles", Slug = "stone-bangles", Description = "Gemstone accents and sparkle", DisplayOrder = 4, ImageUrl = "/images/categories/stone.svg" },
            new() { Name = "Daily Wear", Slug = "daily-wear", Description = "Light, comfortable everyday pieces", DisplayOrder = 5, ImageUrl = "/images/categories/daily.svg" },
            new() { Name = "Premium Collection", Slug = "premium-collection", Description = "Limited, elevated designs", DisplayOrder = 6, ImageUrl = "/images/categories/premium.svg" },
            new() { Name = "Baby & Kids Bangles", Slug = "baby-kids-bangles", Description = "Soft, safe sizes for little wrists", DisplayOrder = 7, ImageUrl = "/images/categories/kids.svg" }
        };

        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();

        var adultSizes = new[] { "2.2", "2.4", "2.6", "2.8" };
        var kidsSizes = new[]
        {
            ("Newborn", "approx. 1.4–1.6"),
            ("0–6 Months", "approx. 1.6–1.8"),
            ("6–12 Months", "approx. 1.8–2.0"),
            ("1–2 Years", "approx. 2.0–2.2"),
            ("2–4 Years", "approx. 2.2–2.4"),
            ("4–6 Years", "approx. 2.4–2.6"),
            ("6–8 Years", "approx. 2.6–2.8"),
            ("8–10 Years", "approx. 2.8–3.0")
        };

        var products = new List<(string Name, string Desc, decimal Price, decimal? Discount, string CatSlug, ProductAgeGroup Age, bool Featured, string Color)>
        {
            ("Aurora Rose Pair", "Delicate rose-gold finish with a soft mirror polish. Ideal for daily elegance.", 3200, 2890, "daily-wear", ProductAgeGroup.Adult, true, "#c9a08a"),
            ("Noor Bridal Set", "Layered bridal set with subtle filigree and warm champagne tone.", 12500, 10990, "bridal-bangles", ProductAgeGroup.Adult, true, "#d4af37"),
            ("Mehndi Heritage Stack", "Traditional stack inspired by festive mehndi looks and heritage motifs.", 4500, null, "traditional-bangles", ProductAgeGroup.Adult, true, "#b87333"),
            ("Crystal Cascade", "Faceted stone accents that catch light with every movement.", 5600, 4990, "stone-bangles", ProductAgeGroup.Adult, true, "#9bb7d4"),
            ("Saffron Gold Classic", "Clean gold-tone classic pair with a refined rounded profile.", 3800, null, "gold-bangles", ProductAgeGroup.Adult, true, "#e6c35c"),
            ("Velvet Premium Duo", "Premium collection duo with a soft brushed finish and balanced weight.", 8900, 7990, "premium-collection", ProductAgeGroup.Adult, true, "#a67c7c"),
            ("Pearl Whisper", "Minimal pearl-inspired daily wear with a quiet luminous sheen.", 2750, null, "daily-wear", ProductAgeGroup.Adult, false, "#e8ddd0"),
            ("Little Bloom Set", "Gentle baby set designed for soft wrists with secure rounded edges.", 1800, 1590, "baby-kids-bangles", ProductAgeGroup.BabyKids, true, "#f0c4c8"),
            ("Tiny Star Kids Pair", "Playful kids pair with a light polished finish and adjustable comfort sizing.", 2100, null, "baby-kids-bangles", ProductAgeGroup.BabyKids, true, "#c5d5e8"),
            ("Royal Mirage Stack", "Bold premium stack for evening wear with rose-gold depth.", 9800, 8990, "premium-collection", ProductAgeGroup.Adult, false, "#b76e79"),
            ("Jasmine Stone Band", "Single stone-accent band that pairs beautifully with stacked looks.", 3400, null, "stone-bangles", ProductAgeGroup.Unisex, false, "#8fbc8f"),
            ("Festive Glow Set", "Warm traditional set for celebrations, gatherings, and gifting.", 6200, 5590, "traditional-bangles", ProductAgeGroup.Adult, false, "#cd853f")
        };

        var productEntities = new List<Product>();
        var rng = new Random(42);
        var index = 1;

        foreach (var p in products)
        {
            var category = categories.First(c => c.Slug == p.CatSlug);
            var slug = Slugify(p.Name);
            var product = new Product
            {
                Name = p.Name,
                Slug = slug,
                Description = p.Desc,
                Price = p.Price,
                DiscountPrice = p.Discount,
                CategoryId = category.Id,
                AgeGroup = p.Age,
                IsFeatured = p.Featured,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-index)
            };

            product.Images.Add(new ProductImage
            {
                ImageUrl = $"/images/products/bangle-{index}.svg",
                AltText = $"{p.Name} — Bareera Bangles",
                IsPrimary = true,
                DisplayOrder = 0
            });
            product.Images.Add(new ProductImage
            {
                ImageUrl = $"/images/products/bangle-{index}-alt.svg",
                AltText = $"{p.Name} alternate view",
                IsPrimary = false,
                DisplayOrder = 1
            });

            if (p.Age == ProductAgeGroup.BabyKids)
            {
                foreach (var (label, measurement) in kidsSizes)
                {
                    product.Sizes.Add(new ProductSize
                    {
                        Size = label,
                        SizeCategory = SizeCategory.BabyKids,
                        Measurement = measurement,
                        StockQuantity = rng.Next(3, 20),
                        IsActive = true
                    });
                }
            }
            else if (p.Age == ProductAgeGroup.Unisex)
            {
                foreach (var size in adultSizes)
                {
                    product.Sizes.Add(new ProductSize
                    {
                        Size = size,
                        SizeCategory = SizeCategory.Unisex,
                        Measurement = $"Inner diameter {size}\"",
                        StockQuantity = rng.Next(5, 25),
                        IsActive = true
                    });
                }
                foreach (var (label, measurement) in kidsSizes.Take(4))
                {
                    product.Sizes.Add(new ProductSize
                    {
                        Size = label,
                        SizeCategory = SizeCategory.BabyKids,
                        Measurement = measurement,
                        StockQuantity = rng.Next(2, 12),
                        IsActive = true
                    });
                }
            }
            else
            {
                foreach (var size in adultSizes)
                {
                    product.Sizes.Add(new ProductSize
                    {
                        Size = size,
                        SizeCategory = SizeCategory.Adult,
                        Measurement = $"Inner diameter {size}\"",
                        StockQuantity = rng.Next(4, 30),
                        IsActive = true
                    });
                }
            }

            productEntities.Add(product);
            index++;
        }

        context.Products.AddRange(productEntities);
        await context.SaveChangesAsync();
        await SeedCouponsIfEmptyAsync(context, logger);

        logger.LogInformation("Seeded {Count} products and {CatCount} categories.", productEntities.Count, categories.Count);
    }

    private static async Task SeedCouponsIfEmptyAsync(ApplicationDbContext context, ILogger logger)
    {
        if (!await context.Coupons.AnyAsync())
        {
            context.Coupons.AddRange(
                new Coupon
                {
                    Code = "BAREERA10",
                    Description = "10% off any order",
                    PercentOff = 10,
                    MinimumSubtotal = 2000,
                    IsActive = true
                },
                new Coupon
                {
                    Code = "WELCOME500",
                    Description = "Rs. 500 off orders over 3000",
                    AmountOff = 500,
                    MinimumSubtotal = 3000,
                    IsActive = true
                },
                new Coupon
                {
                    Code = "FREESHIP",
                    Description = "Free shipping on any order",
                    PercentOff = 0,
                    AmountOff = 0,
                    FreeShipping = true,
                    IsActive = true
                });
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded demo coupons.");
        }

        var freeShip = await context.Coupons.FirstOrDefaultAsync(c => c.Code == "FREESHIP");
        if (freeShip is not null && !freeShip.FreeShipping)
        {
            freeShip.FreeShipping = true;
            freeShip.Description = "Free shipping on any order";
            await context.SaveChangesAsync();
        }
    }

    private static string Slugify(string value)
    {
        var slug = value.ToLowerInvariant();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-").Trim('-');
        return slug;
    }
}
