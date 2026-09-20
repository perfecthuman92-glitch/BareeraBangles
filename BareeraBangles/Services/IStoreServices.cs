using BareeraBangles.Models;
using BareeraBangles.ViewModels;

namespace BareeraBangles.Services;

public interface IProductService
{
    Task<HomeViewModel> GetHomeAsync();
    Task<ShopViewModel> GetShopAsync(string? search, int? categoryId, decimal? minPrice, decimal? maxPrice, string? size, string? ageGroup, string sort, int page, int pageSize);
    Task<ProductDetailsViewModel?> GetBySlugAsync(string slug);
    Task<ProductDetailsViewModel?> GetByIdAsync(int id);
    Task<ProductDetailsViewModel?> GetQuickViewAsync(int id);
    Task<List<Category>> GetActiveCategoriesAsync();
    Task<List<ProductCardViewModel>> SuggestAsync(string term, int take = 8);
}

public interface ICartService
{
    Task<CartViewModel> GetCartAsync();
    Task<(bool Success, string Message)> AddAsync(int productId, int productSizeId, int quantity);
    Task<(bool Success, string Message)> UpdateQuantityAsync(int cartItemId, int quantity);
    Task RemoveAsync(int cartItemId);
    Task ClearAsync();
    Task<int> GetCountAsync();
}

public interface IOrderService
{
    Task<OrderViewModel> CreateOrderAsync(CheckoutViewModel model, string? userId = null);
    Task<OrderViewModel?> GetByOrderNumberAsync(string orderNumber);
    Task<OrderViewModel?> FindByOrderNumberAndPhoneAsync(string orderNumber, string phoneNumber);
    Task<List<OrderViewModel>> GetForUserAsync(string userId);
    Task MarkPaidFromGatewayAsync(string orderNumber, string? providerReference, string? message);
    Task<List<Order>> GetAllOrdersAsync();
    Task<Order?> GetOrderEntityAsync(int id);
    Task UpdateStatusAsync(int orderId, OrderStatus status);
}

public interface IWhatsAppService
{
    string BuildProductOrderUrl(string productName, string? size, int quantity, decimal price);
    string BuildCartOrderUrl(CartViewModel cart);
    string BuildGeneralChatUrl(string? message = null);
    string GetDisplayNumber();
}
