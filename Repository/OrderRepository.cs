using System;
using Contracts;
using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace Repository;

public class OrderRepository : RepositoryBase<Order>, IOrderRepository
{
    public OrderRepository(RepositoryContext repositoryContext) : base(repositoryContext)
    {

    }

    public void CreateOrder(Order order) => Create(order);

    public async Task<IEnumerable<Order>> GetAllOrdersAsync(bool trackChanges) =>
        await FindAll(trackChanges)
        .ToListAsync();

    public async Task<Order> GetOrderAsync(Guid id, bool trackChanges) =>
        await FindByCondition(o => o.Id.Equals(id), trackChanges)
            .Include(o => o.Items)
            .ThenInclude(oi => oi.Product)
            .SingleOrDefaultAsync();

    public async Task<Order> GetOrderByPaymentIntentAsync(string intentId, bool trackChanges) =>
        await FindByCondition(o => o.StripePaymentIntentId.Equals(intentId), trackChanges)
            .Include(o => o.Items)
            .SingleOrDefaultAsync();

    public async Task<Order> GetOrderByPaystackReferenceAsync(string reference, bool trackChanges) =>
        await FindByCondition(o => o.PaystackReference.Equals(reference), trackChanges)
            .Include(o => o.Items)
            .SingleOrDefaultAsync();

    public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(string userId, bool trackChanges) =>
        await FindByCondition(o => o.UserId.Equals(userId), trackChanges)
            .Include(o => o.Items)
            .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public void UpdateOrder(Order order) => Update(order);
}
