using System;
using Entities.Models;

namespace Contracts;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllProductsAsync(string? search, string? category, int page, int pageSize, bool trackChanges);
    Task<Product> GetProductAsync(Guid id, bool trackChanges);
    void CreateProduct(Product product);
    void UpdateProduct(Product product);
    void DeleteProduct(Product product);
}
