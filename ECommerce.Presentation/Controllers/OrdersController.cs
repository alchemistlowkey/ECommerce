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
        /// Checkout: converts the current cart into an order and creates a Stripe PaymentIntent.
        /// Returns a clientSecret for stripe, authorization_url for Paystack, to complete payment.
        /// The PaymentProvider field in the response tells the frontend which flow to use.
        /// </summary>
        [HttpPost("checkout")]
        [ProducesResponseType(typeof(CheckoutResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Checkout()
        {
            var response = await _service.Order.CheckoutAsync(CurrentUserId);
            return Ok(response);
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

        /// <summary>
        /// Stripe webhook endpoint. Called by Stripe after payment events.
        /// Must be excluded from JWT auth — Stripe authenticates via its own signature.
        /// </summary>
        [HttpPost("webhook/stripe")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> StripeWebhook()
        {
            var payload = await new StreamReader(Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"].ToString();

            if (string.IsNullOrWhiteSpace(signature))
                return BadRequest("Missing Stripe-Signature header.");

            await _service.Order.HandleStripeWebhookAsync(payload, signature);
            return Ok();
        }

        /// <summary>
        /// Paystack webhook — receives payment events from Paystack.
        /// Paystack authenticates via HMAC-SHA512 in the x-paystack-signature header.
        /// </summary>
        [HttpPost("webhook/paystack")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PaystackWebhook()
        {
            var payload = await new StreamReader(Request.Body).ReadToEndAsync();
            var signature = Request.Headers["x-paystack-signature"].ToString();

            if (string.IsNullOrWhiteSpace(signature))
                return BadRequest("Missing x-paystack-signature header.");

            await _service.Order.HandlePaystackWebhookAsync(payload, signature);

            // Paystack requires a 200 response within 5 seconds or it will retry.
            return Ok();
        }
    }
}
