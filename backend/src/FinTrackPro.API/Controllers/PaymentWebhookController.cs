using FinTrackPro.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPro.API.Controllers;

[AllowAnonymous]
[Route("api/payment")]
[ApiController]
public class PaymentWebhookController : ControllerBase
{
    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook(
        [FromServices] IPaymentWebhookHandler handler,
        CancellationToken cancellationToken)
    {
        var payload = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);
        // Convert to a provider-neutral dictionary so no ASP.NET Core type leaks into the handler interface.
        var headers = Request.Headers.ToDictionary(
            h => h.Key, h => h.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
        var result = await handler.HandleAsync(payload, headers, cancellationToken);
        return result.SignatureValid ? Ok() : BadRequest();
    }
}
