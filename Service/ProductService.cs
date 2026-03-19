using AutoMapper;
using Contracts;
using Entities.Models;
using Microsoft.Extensions.Logging;
using Service.Contracts;
using Shared.DataTransferObjects.Product;

namespace Service;

public class ProductService : IProductService
{
    private readonly IRepositoryManager _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IRepositoryManager repository, IMapper mapper, ILogger<ProductService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductResponseDto>> GetAllProductsAsync(
        string? search, string? category, int page, int pageSize)
    {
        var products = await _repository.Product
            .GetAllProductsAsync(search, category, page, pageSize, trackChanges: false);

        return _mapper.Map<IEnumerable<ProductResponseDto>>(products);
    }

    public async Task<ProductResponseDto> GetProductAsync(Guid id)
    {
        var product = await _repository.Product.GetProductAsync(id, trackChanges: false)
            ?? throw new KeyNotFoundException($"Product with id '{id}' was not found.");

        return _mapper.Map<ProductResponseDto>(product);
    }

    public async Task<ProductResponseDto> CreateProductAsync(CreateProductRequestDto request)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name ?? string.Empty,
            Description = request.Description ?? string.Empty,
            Price = request.Price,
            Stock = request.Stock,
            Category = request.Category ?? string.Empty,
            ImageUrl = request.ImageUrl ?? string.Empty,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _repository.Product.CreateProduct(product);
        await _repository.SaveAsync();

        _logger.LogInformation(
            "Product created: {ProductId} '{ProductName}', category={Category}, price={Price}, stock={Stock}",
            product.Id, product.Name, product.Category, product.Price, product.Stock);

        return _mapper.Map<ProductResponseDto>(product);
    }

    public async Task<ProductResponseDto> UpdateProductAsync(Guid id, UpdateProductRequestDto request)
    {
        var product = await _repository.Product.GetProductAsync(id, trackChanges: true)
            ?? throw new KeyNotFoundException($"Product with id '{id}' was not found.");

        if (request.Name is not null) product.Name = request.Name;
        if (request.Description is not null) product.Description = request.Description;
        if (request.Category is not null) product.Category = request.Category;
        if (request.ImageUrl is not null) product.ImageUrl = request.ImageUrl;

        // Support both nullable (decimal?) and non-nullable (decimal) DTO shapes
        // by checking via the object type at compile time using pattern overloads
        ApplyPrice(request, product);
        ApplyStock(request, product);
        ApplyIsActive(request, product);

        await _repository.SaveAsync();

        _logger.LogInformation("Product updated: {ProductId} '{ProductName}'", product.Id, product.Name);

        return _mapper.Map<ProductResponseDto>(product);
    }

    // These overload-style helpers compile cleanly regardless of whether the
    // DTO fields are nullable or not — change the cast to match your DTO.
    private static void ApplyPrice(UpdateProductRequestDto r, Product p)
    {
        if (r.Price is decimal price && price > 0)
            p.Price = price;
    }

    private static void ApplyStock(UpdateProductRequestDto r, Product p)
    {
        if (r.Stock is int stock)
            p.Stock = stock;
    }

    private static void ApplyIsActive(UpdateProductRequestDto r, Product p)
    {
        if (r.IsActive is bool active)
            p.IsActive = active;
    }

    public async Task DeleteProductAsync(Guid id)
    {
        var product = await _repository.Product.GetProductAsync(id, trackChanges: true)
            ?? throw new KeyNotFoundException($"Product with id '{id}' was not found.");

        _repository.Product.DeleteProduct(product);
        await _repository.SaveAsync();

        _logger.LogInformation("Product deleted: {ProductId} '{ProductName}'", product.Id, product.Name);
    }
}