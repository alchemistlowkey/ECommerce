using System;
using Shared.DataTransferObjects.Cart;

namespace Service.Contracts;

public interface ICartService
{
    Task<CartResponseDto> GetCartAsync(string userId);
    Task<CartResponseDto> AddItemToCartAsync(string userId, AddToCartRequestDto request);
    Task<CartResponseDto> UpdateCartItemAsync(string userId, Guid itemId, UpdateCartItemRequestDto request);
    Task<CartResponseDto> RemoveCartItemAsync(string userId, Guid itemId);
}
