using BareeraBangles.Areas.Admin.ViewModels;
using BareeraBangles.Data;
using BareeraBangles.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BareeraBangles.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ProductsController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Sizes)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new AdminProductListItem
            {
                Id = p.Id,
                Name = p.Name,
                CategoryName = p.Category!.Name,
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                IsActive = p.IsActive,
                IsFeatured = p.IsFeatured,
                SizeCount = p.Sizes.Count,
                StockTotal = p.Sizes.Sum(s => s.StockQuantity)
            })
            .ToListAsync();

        ViewData["Title"] = "Products";
        return View(items);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateCategoriesAsync();
        var model = new AdminProductEditViewModel
        {
            Sizes = DefaultAdultSizes()
        };
        ViewData["Title"] = "Add Product";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminProductEditViewModel model, IFormFile? primaryImage, IFormFile? secondaryImage)
    {
        await PopulateCategoriesAsync();
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var product = new Product
        {
            Name = model.Name.Trim(),
            Slug = await UniqueSlugAsync(Slugify(model.Name)),
            Description = model.Description.Trim(),
            Price = model.Price,
            DiscountPrice = model.DiscountPrice,
            CategoryId = model.CategoryId,
            AgeGroup = model.AgeGroup,
            IsFeatured = model.IsFeatured,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        var primaryUrl = await SaveImageAsync(primaryImage) ?? model.PrimaryImageUrl ?? "/images/products/placeholder.svg";
        product.Images.Add(new ProductImage
        {
            ImageUrl = primaryUrl,
            AltText = product.Name,
            IsPrimary = true,
            DisplayOrder = 0
        });

        var secondaryUrl = await SaveImageAsync(secondaryImage) ?? model.SecondaryImageUrl;
        if (!string.IsNullOrWhiteSpace(secondaryUrl))
        {
            product.Images.Add(new ProductImage
            {
                ImageUrl = secondaryUrl,
                AltText = $"{product.Name} alternate",
                IsPrimary = false,
                DisplayOrder = 1
            });
        }

        foreach (var size in model.Sizes.Where(s => !s.Remove && !string.IsNullOrWhiteSpace(s.Size)))
        {
            product.Sizes.Add(new ProductSize
            {
                Size = size.Size.Trim(),
                SizeCategory = size.SizeCategory,
                Measurement = size.Measurement,
                StockQuantity = size.StockQuantity,
                IsActive = size.IsActive
            });
        }

        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Product created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var product = await _db.Products
            .Include(p => p.Images)
            .Include(p => p.Sizes)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return NotFound();

        await PopulateCategoriesAsync();
        var model = new AdminProductEditViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            DiscountPrice = product.DiscountPrice,
            CategoryId = product.CategoryId,
            AgeGroup = product.AgeGroup,
            IsFeatured = product.IsFeatured,
            IsActive = product.IsActive,
            PrimaryImageUrl = product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
            SecondaryImageUrl = product.Images.FirstOrDefault(i => !i.IsPrimary)?.ImageUrl,
            Sizes = product.Sizes.Select(s => new AdminSizeEditItem
            {
                Id = s.Id,
                Size = s.Size,
                SizeCategory = s.SizeCategory,
                Measurement = s.Measurement,
                StockQuantity = s.StockQuantity,
                IsActive = s.IsActive
            }).ToList()
        };

        ViewData["Title"] = "Edit Product";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminProductEditViewModel model, IFormFile? primaryImage, IFormFile? secondaryImage)
    {
        if (id != model.Id) return BadRequest();
        await PopulateCategoriesAsync();
        if (!ModelState.IsValid) return View(model);

        var product = await _db.Products
            .Include(p => p.Images)
            .Include(p => p.Sizes)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return NotFound();

        product.Name = model.Name.Trim();
        product.Description = model.Description.Trim();
        product.Price = model.Price;
        product.DiscountPrice = model.DiscountPrice;
        product.CategoryId = model.CategoryId;
        product.AgeGroup = model.AgeGroup;
        product.IsFeatured = model.IsFeatured;
        product.IsActive = model.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        var newPrimary = await SaveImageAsync(primaryImage);
        if (!string.IsNullOrEmpty(newPrimary))
        {
            var primary = product.Images.FirstOrDefault(i => i.IsPrimary);
            if (primary is null)
            {
                product.Images.Add(new ProductImage { ImageUrl = newPrimary, IsPrimary = true, AltText = product.Name });
            }
            else
            {
                primary.ImageUrl = newPrimary;
            }
        }
        else if (!string.IsNullOrWhiteSpace(model.PrimaryImageUrl))
        {
            var primary = product.Images.FirstOrDefault(i => i.IsPrimary);
            if (primary is null)
            {
                product.Images.Add(new ProductImage { ImageUrl = model.PrimaryImageUrl, IsPrimary = true, AltText = product.Name });
            }
            else
            {
                primary.ImageUrl = model.PrimaryImageUrl;
            }
        }

        var newSecondary = await SaveImageAsync(secondaryImage);
        if (!string.IsNullOrEmpty(newSecondary) || !string.IsNullOrWhiteSpace(model.SecondaryImageUrl))
        {
            var url = newSecondary ?? model.SecondaryImageUrl!;
            var secondary = product.Images.FirstOrDefault(i => !i.IsPrimary);
            if (secondary is null)
            {
                product.Images.Add(new ProductImage { ImageUrl = url, IsPrimary = false, AltText = $"{product.Name} alternate", DisplayOrder = 1 });
            }
            else
            {
                secondary.ImageUrl = url;
            }
        }

        foreach (var sizeModel in model.Sizes)
        {
            if (sizeModel.Id > 0)
            {
                var existing = product.Sizes.FirstOrDefault(s => s.Id == sizeModel.Id);
                if (existing is null) continue;
                if (sizeModel.Remove)
                {
                    _db.ProductSizes.Remove(existing);
                    continue;
                }
                existing.Size = sizeModel.Size.Trim();
                existing.SizeCategory = sizeModel.SizeCategory;
                existing.Measurement = sizeModel.Measurement;
                existing.StockQuantity = sizeModel.StockQuantity;
                existing.IsActive = sizeModel.IsActive;
            }
            else if (!sizeModel.Remove && !string.IsNullOrWhiteSpace(sizeModel.Size))
            {
                product.Sizes.Add(new ProductSize
                {
                    Size = sizeModel.Size.Trim(),
                    SizeCategory = sizeModel.SizeCategory,
                    Measurement = sizeModel.Measurement,
                    StockQuantity = sizeModel.StockQuantity,
                    IsActive = sizeModel.IsActive
                });
            }
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Product updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return NotFound();
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Product deactivated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCategoriesAsync()
    {
        ViewBag.Categories = new SelectList(
            await _db.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync(),
            "Id", "Name");
    }

    private static List<AdminSizeEditItem> DefaultAdultSizes() =>
        new[] { "2.2", "2.4", "2.6", "2.8" }
            .Select(s => new AdminSizeEditItem
            {
                Size = s,
                SizeCategory = SizeCategory.Adult,
                Measurement = $"Inner diameter {s}\"",
                StockQuantity = 10,
                IsActive = true
            }).ToList();

    private async Task<string> UniqueSlugAsync(string baseSlug)
    {
        var slug = baseSlug;
        var i = 2;
        while (await _db.Products.AnyAsync(p => p.Slug == slug))
        {
            slug = $"{baseSlug}-{i++}";
        }
        return slug;
    }

    private static string Slugify(string value)
    {
        var slug = value.ToLowerInvariant();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "product" : slug;
    }

    private async Task<string?> SaveImageAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0) return null;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".svg" or ".gif"))
        {
            ModelState.AddModelError(string.Empty, "Unsupported image type.");
            return null;
        }

        var uploads = Path.Combine(_env.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(uploads);
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(uploads, fileName);
        await using var stream = System.IO.File.Create(path);
        await file.CopyToAsync(stream);
        return $"/uploads/products/{fileName}";
    }
}
