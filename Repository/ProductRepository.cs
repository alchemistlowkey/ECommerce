using System;
using Contracts;
using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace Repository;

public class ProductRepository : RepositoryBase<Product>, IProductRepository
{
    public ProductRepository(RepositoryContext repositoryContext) : base(repositoryContext)
    {
    }

    public void CreateProduct(Product product) => Create(product);

    public void DeleteProduct(Product product) => Delete(product);

    public void UpdateProduct(Product product) => Update(product);

    public async Task<IEnumerable<Product>> GetAllProductsAsync(string? search, string? category, int page, int pageSize, bool trackChanges)
    {
        var query = FindAll(trackChanges).Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                p.Name.ToLower().Contains(search.ToLower()) ||
                p.Description.ToLower().Contains(search.ToLower()));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category.ToLower() == category.ToLower());

        return await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Product> GetProductAsync(Guid id, bool trackChanges) =>
        await FindByCondition(p => p.Id.Equals(id), trackChanges)
            .SingleOrDefaultAsync();


}
