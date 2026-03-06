using System;

namespace Contracts;

public interface IRepositoryManager
{
    IUserRepository User { get; }
    IProductRepository Product { get; }
    ICartRepository Cart { get; }
    IOrderRepository Order { get; }
    Task SaveAsync();
}
