using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Shared.DataTransferObjects.Product;

namespace ECommerce.Presentation.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IServiceManager _service;

        public ProductsController(IServiceManager service) => _service = service;

        /// <summary>Get all active products with optional search and category filter.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ProductResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProducts(
            [FromQuery] string? search,
            [FromQuery] string? category,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var products = await _service.Product
                .GetAllProductsAsync(search, category, page, pageSize);
            return Ok(products);
        }

        /// <summary>Get a single product by ID.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProduct(Guid id)
        {
            var product = await _service.Product.GetProductAsync(id);
            return Ok(product);
        }

        /// <summary>Create a new product. Admin only.</summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequestDto request)
        {
            var product = await _service.Product.CreateProductAsync(request);
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        /// <summary>Update an existing product. Admin only.</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequestDto request)
        {
            var product = await _service.Product.UpdateProductAsync(id, request);
            return Ok(product);
        }

        /// <summary>Delete a product. Admin only.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            await _service.Product.DeleteProductAsync(id);
            return NoContent();
        }
    }
}
