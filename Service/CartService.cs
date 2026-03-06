using System;
using AutoMapper;
using Contracts;
using Entities.Models;
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
        var cart = await _repository.Cart.GetCartByUserIdAsync(userId, trackChanges: false);

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
        var product = await _repository.Product.GetProductAsync(request.ProductId, trackChanges: false)
            ?? throw new KeyNotFoundException($"Product with id '{request.ProductId}' was not found.");

        if (!product.IsActive)
            throw new InvalidOperationException("This product is no longer available.");

        if (product.Stock < request.Quantity)
            throw new InvalidOperationException(
                $"Insufficient stock. Requested: {request.Quantity}, Available: {product.Stock}.");

        var cart = await _repository.Cart.GetCartByUserIdAsync(userId, trackChanges: true);

        if (cart is null)
        {
            cart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UpdatedAt = DateTime.Now
            };
            _repository.Cart.CreateCart(cart);
        }

        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (existingItem is not null)
        {
            var newQuantity = existingItem.Quantity + request.Quantity;
            if (product.Stock < newQuantity)
                throw new InvalidOperationException(
                    $"Insufficient stock. Requested total: {newQuantity}, Available: {product.Stock}.");

            existingItem.Quantity = newQuantity;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductId = request.ProductId,
                Quantity = request.Quantity
            });
        }

        cart.UpdatedAt = DateTime.Now;
        await _repository.SaveAsync();

        var updated = await _repository.Cart.GetCartByUserIdAsync(userId, trackChanges: false);

        var cartDto = _mapper.Map<CartResponseDto>(updated!);

        return cartDto;
    }

    public async Task<CartResponseDto> UpdateCartItemAsync(string userId, Guid itemId, UpdateCartItemRequestDto request)
    {
        var cart = await _repository.Cart.GetCartByUserIdAsync(userId, trackChanges: true)
            ?? throw new KeyNotFoundException("Cart not found.");

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new KeyNotFoundException($"Cart item with id '{itemId}' was not found.");

        if (request.Quantity == 0)
        {
            cart.Items.Remove(item);
        }
        else
        {
            var product = await _repository.Product.GetProductAsync(item.ProductId, trackChanges: false)
                ?? throw new KeyNotFoundException("Product not found.");

            if (product.Stock < request.Quantity)
                throw new InvalidOperationException(
                    $"Insufficient stock. Requested: {request.Quantity}, Available: {product.Stock}.");

            item.Quantity = request.Quantity;
        }

        cart.UpdatedAt = DateTime.Now;
        await _repository.SaveAsync();

        var updated = await _repository.Cart.GetCartByUserIdAsync(userId, trackChanges: false);

        var cartDto = _mapper.Map<CartResponseDto>(updated!);

        return cartDto;
    }

    public async Task<CartResponseDto> RemoveCartItemAsync(string userId, Guid itemId)
    {
        var cart = await _repository.Cart.GetCartByUserIdAsync(userId, trackChanges: true)
            ?? throw new KeyNotFoundException("Cart not found.");

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new KeyNotFoundException($"Cart item with id '{itemId}' was not found.");

        cart.Items.Remove(item);
        cart.UpdatedAt = DateTime.Now;
        await _repository.SaveAsync();

        var updated = await _repository.Cart.GetCartByUserIdAsync(userId, trackChanges: false);

        var cartDto = _mapper.Map<CartResponseDto>(updated!);

        return cartDto;
    }
}