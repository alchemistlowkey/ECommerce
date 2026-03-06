using System;
using Entities.Models;

namespace Contracts;

public interface ICartRepository
{
    Task<Cart> GetCartByUserIdAsync(string userId, bool trackChanges);
    void CreateCart(Cart cart);
    void UpdateCart(Cart cart);
}
