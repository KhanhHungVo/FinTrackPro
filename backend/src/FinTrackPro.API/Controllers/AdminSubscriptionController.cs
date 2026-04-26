using FinTrackPro.Application.Admin;
using FinTrackPro.Application.Common.Models;
using FinTrackPro.Application.Subscription.Queries.GetSubscriptionStatus;
using FinTrackPro.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPro.API.Controllers;

[Authorize(Roles = UserRole.Admin)]
[Route("api/admin")]
public class AdminSubscriptionController : BaseApiController
{
    [HttpGet("users")]
    public async Task<ActionResult<PagedResult<AdminUserDto>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? email = null)
        => Ok(await Mediator.Send(new AdminGetUsersQuery(page, pageSize, email)));

    [HttpPost("users/{userId:guid}/subscription")]
    public async Task<ActionResult<SubscriptionStatusDto>> Activate(
        Guid userId, [FromBody] AdminActivateSubscriptionCommand command)
    {
        var result = await Mediator.Send(command with { UserId = userId });
        return Ok(result);
    }

    [HttpDelete("users/{userId:guid}/subscription")]
    public async Task<IActionResult> Revoke(Guid userId)
    {
        await Mediator.Send(new AdminRevokeSubscriptionCommand(userId));
        return NoContent();
    }
}
