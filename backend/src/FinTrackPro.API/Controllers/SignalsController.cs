using FinTrackPro.Application.Signals.Queries.GetSignals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPro.API.Controllers;

[Authorize]
public class SignalsController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SignalDto>>> GetLatest([FromQuery] int count = 20)
        => Ok(await Mediator.Send(new GetSignalsQuery(count)));
}
