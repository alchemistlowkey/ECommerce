using System;
using Entities.Models;

namespace Contracts;

public interface IOrderRepository
{
    Task<IEnumerable<Order>> GetAllOrdersAsync(bool trackChanges);
    Task<IEnumerable<Order>> GetOrdersByUserIdAsync(string userId, bool trackChanges);
    Task<Order> GetOrderAsync(Guid id, bool trackChanges);
    Task<Order?> GetOrderByPaystackReferenceAsync(string reference, bool trackChanges);
    void CreateOrder(Order order);
    void UpdateOrder(Order order);

}
