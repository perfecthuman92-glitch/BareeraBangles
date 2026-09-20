using BareeraBangles.Models;
using BareeraBangles.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace BareeraBangles.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ReviewsController : Controller
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Reviews";
        return View(await _reviewService.GetPendingAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        await _reviewService.ApproveAsync(id, true);
        TempData["Success"] = "Review approved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        await _reviewService.ApproveAsync(id, false);
        TempData["Success"] = "Review removed.";
        return RedirectToAction(nameof(Index));
    }
}
