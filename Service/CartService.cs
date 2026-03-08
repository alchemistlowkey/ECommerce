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
        _mapper     = mapper;
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<CartResponseDto> ReadCartAsync(string userId)
    {
        var cart = await _repository.Cart
            .GetCartByUserIdAsync(userId, trackChanges: false);

        if (cart is null)
            return new CartResponseDto
            {
                Id    = Guid.Empty,
                Items = new List<CartItemResponseDto>(),
                Total = 0m
            };

        return _mapper.Map<CartResponseDto>(cart);
    }

    // Ensure a Cart row exists for the user and return its Id.
    // Uses a direct INSERT that is safe to call even if the row already exists
    // because it checks first — no EF change tracker involved.
    private async Task<Guid> EnsureCartAsync(string userId)
    {
        var existing = await _repository.Cart
            .GetCartByUserIdAsync(userId, trackChanges: false);

        if (existing is not null)
            return existing.Id;

        var cart = new Cart
        {
            Id        = Guid.NewGuid(),
            UserId    = userId,
            UpdatedAt = DateTime.UtcNow
        };

        _repository.Cart.CreateCart(cart);
        await _repository.SaveAsync();
        return cart.Id;
    }

    // ── public operations ────────────────────────────────────────────────────

    public Task<CartResponseDto> GetCartAsync(string userId) =>
        ReadCartAsync(userId);

    public async Task<CartResponseDto> AddItemToCartAsync(
        string userId, AddToCartRequestDto request)
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

        var cartId = await EnsureCartAsync(userId);

        // Check if item already exists — read-only, no tracking
        var freshCart = await _repository.Cart
            .GetCartByUserIdAsync(userId, trackChanges: false);

        var existing = freshCart?.Items
            .FirstOrDefault(i => i.ProductId == productId);

        if (existing is not null)
        {
            var newQty = existing.Quantity + request.Quantity;
            if (product.Stock < newQty)
                throw new InvalidOperationException(
                    $"Insufficient stock. Requested total: {newQty}, Available: {product.Stock}.");

            // Direct SQL UPDATE — no change tracker, no concurrency token
            await _repository.Cart.UpdateCartItemQuantityAsync(existing.Id, newQty);
        }
        else
        {
            // Direct SQL INSERT — no change tracker
            await _repository.Cart.AddCartItemDirectAsync(new CartItem
            {
                Id        = Guid.NewGuid(),
                CartId    = cartId,
                ProductId = productId,
                Quantity  = request.Quantity
            });
        }

        await _repository.Cart.TouchCartAsync(cartId);
        return await ReadCartAsync(userId);
    }

    public async Task<CartResponseDto> UpdateCartItemAsync(
        string userId, Guid itemId, UpdateCartItemRequestDto request)
    {
        var cart = await _repository.Cart
            .GetCartByUserIdAsync(userId, trackChanges: false)
            ?? throw new KeyNotFoundException("Cart not found.");

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new KeyNotFoundException($"Cart item '{itemId}' was not found.");

        if (request.Quantity == 0)
        {
            await _repository.Cart.RemoveCartItemAsync(itemId);
        }
        else
        {
            var product = await _repository.Product
                .GetProductAsync(item.ProductId, trackChanges: false)
                ?? throw new KeyNotFoundException("Product not found.");

            if (product.Stock < request.Quantity)
                throw new InvalidOperationException(
                    $"Insufficient stock. Requested: {request.Quantity}, Available: {product.Stock}.");

            await _repository.Cart.UpdateCartItemQuantityAsync(itemId, request.Quantity);
        }

        await _repository.Cart.TouchCartAsync(cart.Id);
        return await ReadCartAsync(userId);
    }

    public async Task<CartResponseDto> RemoveCartItemAsync(string userId, Guid itemId)
    {
        var cart = await _repository.Cart
            .GetCartByUserIdAsync(userId, trackChanges: false)
            ?? throw new KeyNotFoundException("Cart not found.");

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new KeyNotFoundException($"Cart item '{itemId}' was not found.");

        await _repository.Cart.RemoveCartItemAsync(item.Id);
        await _repository.Cart.TouchCartAsync(cart.Id);
        return await ReadCartAsync(userId);
    }
}