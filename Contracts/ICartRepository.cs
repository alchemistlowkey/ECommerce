using Entities.Models;

namespace Contracts;

public interface ICartRepository
{
    Task<Cart?> GetCartByUserIdAsync(string userId, bool trackChanges);
    void CreateCart(Cart cart);
    void UpdateCart(Cart cart);
    Task AddCartItemDirectAsync(CartItem item);
    Task UpdateCartItemQuantityAsync(Guid itemId, int quantity);
    Task RemoveCartItemAsync(Guid itemId);
    Task TouchCartAsync(Guid cartId);
    Task DeleteCartAsync(Guid cartId);   // clears all items + removes cart row
}