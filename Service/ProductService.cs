using AutoMapper;
using Contracts;
using Entities.Models;
using Service.Contracts;
using Shared.DataTransferObjects.Product;

namespace Service;

public class ProductService : IProductService
{
    private readonly IRepositoryManager _repository;
    private readonly IMapper _mapper;

    public ProductService(IRepositoryManager repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
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
        var product = await _repository.Product
            .GetProductAsync(id, trackChanges: false)
            ?? throw new KeyNotFoundException($"Product with id '{id}' was not found.");

        return _mapper.Map<ProductResponseDto>(product);
    }

    public async Task<ProductResponseDto> CreateProductAsync(CreateProductRequestDto request)
    {
        var product = _mapper.Map<Product>(request);
        product.Id = Guid.NewGuid();
        product.CreatedAt = DateTime.UtcNow;

        _repository.Product.CreateProduct(product);
        await _repository.SaveAsync();

        return _mapper.Map<ProductResponseDto>(product);
    }

    public async Task<ProductResponseDto> UpdateProductAsync(Guid id, UpdateProductRequestDto request)
    {
        var product = await _repository.Product
            .GetProductAsync(id, trackChanges: true)
            ?? throw new KeyNotFoundException($"Product with id '{id}' was not found.");

        _mapper.Map(request, product);
        await _repository.SaveAsync();

        return _mapper.Map<ProductResponseDto>(product);
    }

    public async Task DeleteProductAsync(Guid id)
    {
        var product = await _repository.Product
            .GetProductAsync(id, trackChanges: true)
            ?? throw new KeyNotFoundException($"Product with id '{id}' was not found.");

        _repository.Product.DeleteProduct(product);
        await _repository.SaveAsync();
    }
}