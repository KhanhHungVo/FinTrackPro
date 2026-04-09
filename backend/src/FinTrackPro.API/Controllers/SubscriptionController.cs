using FinTrackPro.Application.Subscription.Commands.CreateBillingPortalSession;
using FinTrackPro.Application.Subscription.Commands.CreateCheckoutSession;
using FinTrackPro.Application.Subscription.Queries.GetSubscriptionStatus;
using FinTrackPro.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPro.API.Controllers;

[Authorize(Roles = UserRole.User)]
public class SubscriptionController : BaseApiController
{
    [HttpGet("status")]
    public async Task<ActionResult<SubscriptionStatusDto>> GetStatus()
        => Ok(await Mediator.Send(new GetSubscriptionStatusQuery()));

    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutSessionDto>> CreateCheckout(
        [FromBody] CreateCheckoutSessionCommand command)
        => Ok(await Mediator.Send(command));

    [HttpPost("portal")]
    public async Task<ActionResult<BillingPortalSessionDto>> CreatePortal(
        [FromBody] CreateBillingPortalSessionCommand command)
        => Ok(await Mediator.Send(command));
}
