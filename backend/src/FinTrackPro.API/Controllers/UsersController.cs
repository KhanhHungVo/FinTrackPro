using FinTrackPro.Application.Users.Commands.UpdateUserPreferences;
using FinTrackPro.Application.Users.Queries.GetUserPreferences;
using FinTrackPro.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPro.API.Controllers;

[Authorize(Roles = UserRole.User)]
public class UsersController : BaseApiController
{
    [HttpGet("preferences")]
    public async Task<ActionResult<UserPreferencesDto>> GetPreferences(CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetUserPreferencesQuery(), cancellationToken));

    [HttpPatch("preferences")]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdateUserPreferencesCommand command,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
