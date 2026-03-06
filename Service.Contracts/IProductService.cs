using System;
using Shared.DataTransferObjects.Product;

namespace Service.Contracts;

public interface IProductService
{
    Task<IEnumerable<ProductResponseDto>> GetAllProductsAsync(
           string? search, string? category, int page, int pageSize);
    Task<ProductResponseDto> GetProductAsync(Guid id);
    Task<ProductResponseDto> CreateProductAsync(CreateProductRequestDto request);
    Task<ProductResponseDto> UpdateProductAsync(Guid id, UpdateProductRequestDto request);
    Task DeleteProductAsync(Guid id);
}
