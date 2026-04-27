using FinTrackPro.Application.Signals.Commands.DismissSignal;
using FinTrackPro.Application.Signals.Queries.GetSignals;
using FinTrackPro.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPro.API.Controllers;

[Authorize(Roles = UserRole.User)]
public class SignalsController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SignalDto>>> GetLatest([FromQuery] int count = 20)
        => Ok(await Mediator.Send(new GetSignalsQuery(count)));

    [HttpPatch("{id:guid}/dismiss")]
    public async Task<IActionResult> Dismiss(Guid id)
    {
        await Mediator.Send(new DismissSignalCommand(id));
        return NoContent();
    }
}
