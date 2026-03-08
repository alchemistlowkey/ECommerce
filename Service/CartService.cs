using AutoMapper;
using Contracts;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using Shared.DataTransferObjects.Cart;

namespace Service;

public class CartService : ICartService
{
    private readonly IRepositoryManager _repository;
    private readonly IMapper _mapper;

    public CartService(IRepositoryManager repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<CartResponseDto> GetCartAsync(string userId)
    {
        var cart = await _repository.Cart
            .GetCartByUserIdAsync(userId, trackChanges: false);

        if (cart is null)
            return new CartResponseDto
            {
                Id = Guid.Empty,
                Items = new List<CartItemResponseDto>(),
                Total = 0m
            };

        return _mapper.Map<CartResponseDto>(cart);
    }

    public async Task<CartResponseDto> AddItemToCartAsync(string userId, AddToCartRequestDto request)
    {
        var productId = request.ProductId!.Value;

        var product = await _repository.Product
            .GetProductAsync(productId, trackChanges: false)
            ?? throw new KeyNotFoundException($"Product '{productId}' was not found.");

        if (!product.IsActive)
            throw new InvalidOperationException("This product is no longer available.");

        if (product.Stock < request.Quantity)
            throw new InvalidOperationException(
                $"Insufficient stock. Requested: {request.Quantity}, Available: {product.Stock}.");

        // Always fetch with tracking so EF can persist changes
        var cart = await _repository.Cart
            .GetCartByUserIdAsync(userId, trackChanges: true);

        if (cart is null)
        {
            // Build the cart and its first item together so EF inserts them
            // in one round-trip with the FK relationship already satisfied.
            cart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UpdatedAt = DateTime.UtcNow,
                Items = new List<CartItem>
                {
                    new CartItem
                    {
                        Id        = Guid.NewGuid(),
                        ProductId = productId,
                        Quantity  = request.Quantity
                    }
                }
            };

            _repository.Cart.CreateCart(cart);
            await _repository.SaveAsync();
        }
        else
        {
            var existingItem = cart.Items
                .FirstOrDefault(i => i.ProductId == productId);

            if (existingItem is not null)
            {
                var newQty = existingItem.Quantity + request.Quantity;
                if (product.Stock < newQty)
                    throw new InvalidOperationException(
                        $"Insufficient stock. Requested total: {newQty}, Available: {product.Stock}.");

                existingItem.Quantity = newQty;
            }
            else
            {
                cart.Items.Add(new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = request.Quantity
                });
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _repository.SaveAsync();
        }

        // Re-fetch with includes so AutoMapper can resolve ProductName/UnitPrice
        var updated = await _repository.Cart
            .GetCartByUserIdAsync(userId, trackChanges: false);

        return _mapper.Map<CartResponseDto>(updated!);
    }

    public async Task<CartResponseDto> UpdateCartItemAsync(
        string userId, Guid itemId, UpdateCartItemRequestDto request)
    {
        var cart = await _repository.Cart
            .GetCartByUserIdAsync(userId, trackChanges: true)
            ?? throw new KeyNotFoundException("Cart not found.");

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new KeyNotFoundException($"Cart item '{itemId}' was not found.");

        if (request.Quantity == 0)
        {
            cart.Items.Remove(item);
        }
        else
        {
            var product = await _repository.Product
                .GetProductAsync(item.ProductId, trackChanges: false)
                ?? throw new KeyNotFoundException("Product not found.");

            if (product.Stock < request.Quantity)
                throw new InvalidOperationException(
                    $"Insufficient stock. Requested: {request.Quantity}, Available: {product.Stock}.");

            item.Quantity = request.Quantity;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _repository.SaveAsync();

        var updated = await _repository.Cart
            .GetCartByUserIdAsync(userId, trackChanges: false);

        return _mapper.Map<CartResponseDto>(updated!);
    }

    public async Task<CartResponseDto> RemoveCartItemAsync(string userId, Guid itemId)
    {
        var cart = await _repository.Cart
            .GetCartByUserIdAsync(userId, trackChanges: true)
            ?? throw new KeyNotFoundException("Cart not found.");

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new KeyNotFoundException($"Cart item '{itemId}' was not found.");

        cart.Items.Remove(item);
        cart.UpdatedAt = DateTime.UtcNow;
        await _repository.SaveAsync();

        var updated = await _repository.Cart
            .GetCartByUserIdAsync(userId, trackChanges: false);

        return _mapper.Map<CartResponseDto>(updated!);
    }
}