using System;
using Contracts;

namespace Repository;

public class RepositoryManager : IRepositoryManager
{
    private readonly RepositoryContext _repositoryContext;
    private readonly Lazy<IUserRepository> _userRepository;
    private readonly Lazy<IProductRepository> _productRepository;
    private readonly Lazy<ICartRepository> _cartRepository;
    private readonly Lazy<IOrderRepository> _orderRepository;

    public RepositoryManager(RepositoryContext repositoryContext)
    {
        _repositoryContext = repositoryContext;
        _userRepository = new Lazy<IUserRepository>(() => new UserRepository(_repositoryContext));
        _productRepository = new Lazy<IProductRepository>(() => new ProductRepository(_repositoryContext));
        _cartRepository = new Lazy<ICartRepository>(() => new CartRepository(_repositoryContext));
        _orderRepository = new Lazy<IOrderRepository>(() => new OrderRepository(_repositoryContext));
    }
    public IUserRepository User => _userRepository.Value;

    public IProductRepository Product => _productRepository.Value;

    public ICartRepository Cart => _cartRepository.Value;

    public IOrderRepository Order => _orderRepository.Value;

    public Task SaveAsync() => _repositoryContext.SaveChangesAsync();
}
