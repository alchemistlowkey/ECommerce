using System;
using AutoMapper;
using Contracts;
using Entities.ConfigurationModels;
using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Service.Contracts;

namespace Service;

public class ServiceManager : IServiceManager
{
    private readonly Lazy<ProductService> _productService;
    private readonly Lazy<CartService> _cartService;
    private readonly Lazy<OrderService> _orderService;
    private readonly Lazy<AuthService> _authService;

    public ServiceManager(
        IRepositoryManager repositoryManager,
        IMapper mapper,
        UserManager<User> userManager,
        IOptions<JwtConfiguration> configuration,
        IServiceProvider services,
        ILoggerFactory loggerFactory)
    {
        _productService = new Lazy<ProductService>(() =>
            new ProductService(
                repositoryManager,
                mapper,
                loggerFactory.CreateLogger<ProductService>()));

        _cartService = new Lazy<CartService>(() =>
            new CartService(repositoryManager, mapper));

        _orderService = new Lazy<OrderService>(() =>
            new OrderService(
                repositoryManager,
                mapper,
                services,
                loggerFactory.CreateLogger<OrderService>()));

        _authService = new Lazy<AuthService>(() =>
            new AuthService(
                userManager,
                configuration,
                loggerFactory.CreateLogger<AuthService>()));
    }

    public IAuthService Auth => _authService.Value;
    public IProductService Product => _productService.Value;
    public ICartService Cart => _cartService.Value;
    public IOrderService Order => _orderService.Value;
}