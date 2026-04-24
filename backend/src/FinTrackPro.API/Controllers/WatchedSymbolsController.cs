using FinTrackPro.Application.Trading.Commands.AddWatchedSymbol;
using FinTrackPro.Application.Trading.Commands.RemoveWatchedSymbol;
using FinTrackPro.Application.Trading.Queries.GetWatchedSymbols;
using FinTrackPro.Application.Trading.Queries.GetWatchlistAnalysis;
using FinTrackPro.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPro.API.Controllers;

[Authorize(Roles = UserRole.User)]
public class WatchedSymbolsController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WatchedSymbolDto>>> GetAll()
        => Ok(await Mediator.Send(new GetWatchedSymbolsQuery()));

    [HttpPost]
    public async Task<ActionResult<Guid>> Add(AddWatchedSymbolCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), new { }, id);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id)
    {
        await Mediator.Send(new RemoveWatchedSymbolCommand(id));
        return NoContent();
    }

    [HttpGet("analysis")]
    public async Task<ActionResult<IEnumerable<WatchlistAnalysisItemDto>>> GetAnalysis(CancellationToken ct)
        => Ok(await Mediator.Send(new GetWatchlistAnalysisQuery(), ct));
}
