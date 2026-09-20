using BareeraBangles.Services;
using Microsoft.AspNetCore.Mvc;

namespace BareeraBangles.Controllers;

public class CompareController : Controller
{
    private readonly ICompareService _compareService;

    public CompareController(ICompareService compareService)
    {
        _compareService = compareService;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Compare";
        return View(await _compareService.GetAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int productId)
    {
        var (success, message, count) = await _compareService.ToggleAsync(productId);
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success, message, count });
        }

        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int productId)
    {
        await _compareService.RemoveAsync(productId);
        TempData["Success"] = "Removed from compare.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear()
    {
        await _compareService.ClearAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Count()
    {
        return Json(new { count = await _compareService.GetCountAsync() });
    }
}

public class HealthController : Controller
{
    [HttpGet("/health")]
    public IActionResult Index()
    {
        return Json(new
        {
            status = "Healthy",
            store = "Bareera Bangles",
            utc = DateTime.UtcNow
        });
    }
}
