using System;
using Contracts;
using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace Repository;

public class CartRepository : RepositoryBase<Cart>, ICartRepository
{
    public CartRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }

    public void CreateCart(Cart cart) => Create(cart);

    public async Task<Cart> GetCartByUserIdAsync(string userId, bool trackChanges) =>
        await FindByCondition(c => c.UserId.Equals(userId), trackChanges)
            .Include(c => c.Items)
            .ThenInclude(ci => ci.Product)
            .SingleOrDefaultAsync();

    public void UpdateCart(Cart cart) => Update(cart);
}
