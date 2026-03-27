using FinTrackPro.Application.Trading.Commands.CreateTrade;
using FinTrackPro.Application.Trading.Commands.DeleteTrade;
using FinTrackPro.Application.Trading.Commands.UpdateTrade;
using FinTrackPro.Application.Trading.Queries.GetTrades;
using FinTrackPro.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPro.API.Controllers;

[Authorize(Roles = UserRole.User)]
public class TradesController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TradeDto>>> GetAll()
        => Ok(await Mediator.Send(new GetTradesQuery()));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateTradeCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), new { }, id);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TradeDto>> Update(Guid id, UpdateTradeCommand command)
    {
        var result = await Mediator.Send(command with { Id = id });
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteTradeCommand(id));
        return NoContent();
    }
}
