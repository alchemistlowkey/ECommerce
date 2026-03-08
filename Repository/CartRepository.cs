using Contracts;
using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace Repository;

public class CartRepository : RepositoryBase<Cart>, ICartRepository
{
    private readonly RepositoryContext _db;

    public CartRepository(RepositoryContext context) : base(context)
    {
        _db = context;
    }

    public async Task<Cart?> GetCartByUserIdAsync(string userId, bool trackChanges) =>
        await FindByCondition(c => c.UserId == userId, trackChanges)
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .SingleOrDefaultAsync();

    public void CreateCart(Cart cart) => Create(cart);
    public void UpdateCart(Cart cart) => Update(cart);

    public async Task AddCartItemDirectAsync(CartItem item)
    {
        _db.Set<CartItem>().Add(item);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateCartItemQuantityAsync(Guid itemId, int quantity) =>
        await _db.Set<CartItem>()
            .Where(i => i.Id == itemId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.Quantity, quantity));

    public async Task RemoveCartItemAsync(Guid itemId) =>
        await _db.Set<CartItem>()
            .Where(i => i.Id == itemId)
            .ExecuteDeleteAsync();

    public async Task TouchCartAsync(Guid cartId) =>
        await _db.Set<Cart>()
            .Where(c => c.Id == cartId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.UpdatedAt, DateTime.UtcNow));

    public async Task DeleteCartAsync(Guid cartId)
    {
        await _db.Set<CartItem>()
            .Where(i => i.CartId == cartId)
            .ExecuteDeleteAsync();
        await _db.Set<Cart>()
            .Where(c => c.Id == cartId)
            .ExecuteDeleteAsync();
    }
}