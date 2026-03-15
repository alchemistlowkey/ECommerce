using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Shared.DataTransferObjects.Order;

namespace ECommerce.Presentation.Controllers
{
    [Route("api/orders")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IServiceManager _service;

        public OrdersController(IServiceManager service) => _service = service;

        private string CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User Identity not found.");

        /// <summary>
        /// Checkout: converts the current cart into an order and creates a payment
        /// authorization URL. Returns the provider's hosted payment page URL.
        /// </summary>
        [HttpPost("checkout")]
        [ProducesResponseType(typeof(CheckoutResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDto request)
        {
            var response = await _service.Order.CheckoutAsync(CurrentUserId, request.PaymentProvider);
            return Ok(response);
        }

        /// <summary>
        /// Verify payment status for an order by actively querying the payment provider.
        /// Called by the frontend immediately after the user returns from the payment page
        /// so the order status is updated without waiting for a webhook.
        /// </summary>
        [HttpPost("{id:guid}/verify-payment")]
        [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> VerifyPayment(Guid id)
        {
            var order = await _service.Order.VerifyPaymentAsync(CurrentUserId, id);
            return Ok(order);
        }

        /// <summary>Get all orders for the current user.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<OrderResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _service.Order.GetUserOrdersAsync(CurrentUserId);
            return Ok(orders);
        }

        /// <summary>Get a specific order by ID.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetOrder(Guid id)
        {
            var order = await _service.Order.GetOrderAsync(CurrentUserId, id);
            return Ok(order);
        }
    }
}
