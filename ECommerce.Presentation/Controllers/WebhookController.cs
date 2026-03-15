using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;

namespace ECommerce.Presentation.Controllers
{
    /// <summary>
    /// Handles incoming webhooks from payment providers (Paystack and Flutterwave).
    ///
    /// These endpoints MUST NOT inherit [Authorize] from a parent controller — they are
    /// called by external servers that have no JWT token. Putting them in their own
    /// controller (not nested under OrdersController which is [Authorize]) ensures
    /// [AllowAnonymous] is not shadowed by a class-level [Authorize] attribute.
    ///
    /// The webhook 401 bug was caused by the endpoints living inside the [Authorize]
    /// OrdersController. ASP.NET Core's JWT middleware challenges the request before
    /// the [AllowAnonymous] attribute is evaluated in some pipeline configurations.
    /// Moving them here eliminates that ambiguity entirely.
    /// </summary>
    [Route("api/orders")]
    [ApiController]
    [AllowAnonymous]
    public class WebhookController : ControllerBase
    {
        private readonly IServiceManager _service;

        public WebhookController(IServiceManager service) => _service = service;

        /// <summary>
        /// Flutterwave webhook endpoint. Called by Flutterwave after payment events.
        /// Authenticated via the "verif-hash" header (plain secret string comparison).
        /// </summary>
        [HttpPost("webhook/flutterwave")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> FlutterwaveWebhook()
        {
            var payload = await new StreamReader(Request.Body).ReadToEndAsync();
            var signature = Request.Headers["verif-hash"].ToString();

            if (string.IsNullOrWhiteSpace(signature))
                return BadRequest("Missing verif-hash header.");

            await _service.Order.HandleFlutterwaveWebhookAsync(payload, signature);
            return Ok();
        }

        /// <summary>
        /// Paystack webhook endpoint. Called by Paystack after payment events.
        /// Authenticated via HMAC-SHA512 in the "x-paystack-signature" header.
        /// Paystack requires a 200 response within 5 seconds or it retries.
        /// </summary>
        [HttpPost("webhook/paystack")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PaystackWebhook()
        {
            var payload = await new StreamReader(Request.Body).ReadToEndAsync();
            var signature = Request.Headers["x-paystack-signature"].ToString();

            if (string.IsNullOrWhiteSpace(signature))
                return BadRequest("Missing x-paystack-signature header.");

            await _service.Order.HandlePaystackWebhookAsync(payload, signature);
            return Ok();
        }
    }
}
