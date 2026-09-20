using BareeraBangles.ViewModels;

namespace BareeraBangles.Services;

public interface IWishlistService
{
    Task<List<ProductCardViewModel>> GetItemsAsync();
    Task<(bool Success, string Message, int Count)> ToggleAsync(int productId);
    Task RemoveAsync(int productId);
    Task<int> GetCountAsync();
    Task<HashSet<int>> GetProductIdsAsync();
}
