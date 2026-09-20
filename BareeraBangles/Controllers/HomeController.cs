using BareeraBangles.Configuration;
using BareeraBangles.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BareeraBangles.Controllers;

public class HomeController : Controller
{
    private readonly IProductService _productService;
    private readonly IRecentlyViewedService _recentlyViewed;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IProductService productService,
        IRecentlyViewedService recentlyViewed,
        ILogger<HomeController> logger)
    {
        _productService = productService;
        _recentlyViewed = recentlyViewed;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var model = await _productService.GetHomeAsync();
        model.RecentlyViewed = await _recentlyViewed.GetAsync(4);
        ViewData["Title"] = "Home";
        ViewData["MetaDescription"] = "Bareera Bangles — elegant adult and baby bangles with WhatsApp ordering and secure checkout.";
        return View(model);
    }

    public IActionResult Privacy()
    {
        ViewData["Title"] = "Privacy Policy";
        return View();
    }

    public IActionResult Terms()
    {
        ViewData["Title"] = "Terms & Conditions";
        return View();
    }

    public IActionResult About()
    {
        ViewData["Title"] = "About";
        return View();
    }

    public IActionResult Contact()
    {
        ViewData["Title"] = "Contact";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(
        string name,
        string email,
        string message,
        [FromServices] IEmailService emailService,
        [FromServices] IOptions<StoreSettings> storeOptions)
    {
        ViewData["Title"] = "Contact";
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message) || !email.Contains('@'))
        {
            TempData["Error"] = "Please fill in your name, a valid email, and a message.";
            return View();
        }

        var store = storeOptions.Value;
        var body = $"<p><strong>From:</strong> {System.Net.WebUtility.HtmlEncode(name)} &lt;{System.Net.WebUtility.HtmlEncode(email)}&gt;</p>" +
                   $"<p>{System.Net.WebUtility.HtmlEncode(message)}</p>";
        await emailService.SendAsync(store.SupportEmail, $"Contact form — {name.Trim()}", body);
        TempData["Success"] = "Thanks — your message was sent. We’ll reply soon.";
        return RedirectToAction(nameof(Contact));
    }

    public IActionResult SizeGuide()
    {
        ViewData["Title"] = "Size Guide";
        ViewData["MetaDescription"] = "Adult bangle diameter guide and baby/kids age-based size labels for Bareera Bangles.";
        return View();
    }

    public IActionResult Faq()
    {
        ViewData["Title"] = "FAQ";
        ViewData["MetaDescription"] = "Frequently asked questions about Bareera Bangles sizing, shipping, WhatsApp orders, and payments.";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Newsletter(string email, [FromServices] Data.ApplicationDbContext db)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            TempData["Error"] = "Please enter a valid email.";
            return RedirectToAction(nameof(Index));
        }

        email = email.Trim().ToLowerInvariant();
        var exists = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .AnyAsync(db.NewsletterSubscribers, s => s.Email == email);
        if (!exists)
        {
            db.NewsletterSubscribers.Add(new Models.NewsletterSubscriber { Email = email });
            await db.SaveChangesAsync();
        }

        TempData["Success"] = "Thanks for subscribing!";
        return RedirectToAction(nameof(Index));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new Models.ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
