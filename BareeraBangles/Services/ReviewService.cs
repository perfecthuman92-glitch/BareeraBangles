using System.ComponentModel.DataAnnotations;
using BareeraBangles.Data;
using BareeraBangles.Models;
using Microsoft.EntityFrameworkCore;

namespace BareeraBangles.Services;

public class ReviewViewModel
{
    public int Id { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class SubmitReviewRequest
{
    public int ProductId { get; set; }

    [Required, MaxLength(100)]
    public string ReviewerName { get; set; } = string.Empty;

    [Range(1, 5)]
    public int Rating { get; set; } = 5;

    [Required, MaxLength(1000)]
    public string Comment { get; set; } = string.Empty;
}

public interface IReviewService
{
    Task<List<ReviewViewModel>> GetApprovedForProductAsync(int productId);
    Task<(double Average, int Count)> GetStatsAsync(int productId);
    Task<(bool Success, string Message)> SubmitAsync(SubmitReviewRequest request);
    Task<List<ProductReview>> GetPendingAsync();
    Task ApproveAsync(int id, bool approved);
}

public class ReviewService : IReviewService
{
    private readonly ApplicationDbContext _db;

    public ReviewService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<ReviewViewModel>> GetApprovedForProductAsync(int productId)
    {
        return await _db.ProductReviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId && r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewViewModel
            {
                Id = r.Id,
                ReviewerName = r.ReviewerName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<(double Average, int Count)> GetStatsAsync(int productId)
    {
        var q = _db.ProductReviews.AsNoTracking().Where(r => r.ProductId == productId && r.IsApproved);
        var count = await q.CountAsync();
        if (count == 0) return (0, 0);
        var avg = await q.AverageAsync(r => (double)r.Rating);
        return (Math.Round(avg, 1), count);
    }

    public async Task<(bool Success, string Message)> SubmitAsync(SubmitReviewRequest request)
    {
        if (!await _db.Products.AnyAsync(p => p.Id == request.ProductId && p.IsActive))
        {
            return (false, "Product not found.");
        }

        _db.ProductReviews.Add(new ProductReview
        {
            ProductId = request.ProductId,
            ReviewerName = request.ReviewerName.Trim(),
            Rating = request.Rating,
            Comment = request.Comment.Trim(),
            IsApproved = false,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return (true, "Thank you! Your review will appear after approval.");
    }

    public Task<List<ProductReview>> GetPendingAsync()
    {
        return _db.ProductReviews
            .Include(r => r.Product)
            .Where(r => !r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task ApproveAsync(int id, bool approved)
    {
        var review = await _db.ProductReviews.FindAsync(id);
        if (review is null) return;
        if (approved)
        {
            review.IsApproved = true;
        }
        else
        {
            _db.ProductReviews.Remove(review);
        }
        await _db.SaveChangesAsync();
    }
}
