using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Shared.DataTransferObjects.Cart;

namespace ECommerce.Presentation.Controllers
{
    [Route("api/cart")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly IServiceManager _service;

        public CartController(IServiceManager service) => _service = service;

        private string CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User Identity not found.");

        /// <summary>Get the current user's cart.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(CartResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCart()
        {
            var cart = await _service.Cart.GetCartAsync(CurrentUserId);
            return Ok(cart);
        }

        /// <summary>Add a product to the cart.</summary>
        [HttpPost("items")]
        [ProducesResponseType(typeof(CartResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddItem([FromBody] AddToCartRequestDto request)
        {
            var cart = await _service.Cart.AddItemToCartAsync(CurrentUserId, request);
            return Ok(cart);
        }

        /// <summary>Update the quantity of a cart item. Set quantity to 0 to remove.</summary>
        [HttpPut("items/{itemId:guid}")]
        [ProducesResponseType(typeof(CartResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateItem(Guid itemId, [FromBody] UpdateCartItemRequestDto request)
        {
            var cart = await _service.Cart.UpdateCartItemAsync(CurrentUserId, itemId, request);
            return Ok(cart);
        }

        /// <summary>Remove a specific item from the cart.</summary>
        [HttpDelete("items/{itemId:guid}")]
        [ProducesResponseType(typeof(CartResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveItem(Guid itemId)
        {
            var cart = await _service.Cart.RemoveCartItemAsync(CurrentUserId, itemId);
            return Ok(cart);
        }
    }
}
