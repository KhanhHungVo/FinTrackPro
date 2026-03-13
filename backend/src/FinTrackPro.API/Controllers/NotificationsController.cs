using FinTrackPro.Application.Notifications.Commands.SaveNotificationPreference;
using FinTrackPro.Application.Notifications.Queries.GetNotificationPreference;
using FinTrackPro.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPro.API.Controllers;

[Authorize(Roles = UserRole.User)]
public class NotificationsController : BaseApiController
{
    [HttpGet("preferences")]
    public async Task<ActionResult<NotificationPreferenceDto?>> GetPreferences()
        => Ok(await Mediator.Send(new GetNotificationPreferenceQuery()));

    [HttpPost("preferences")]
    public async Task<IActionResult> SavePreferences(SaveNotificationPreferenceCommand command)
    {
        await Mediator.Send(command);
        return NoContent();
    }
}
