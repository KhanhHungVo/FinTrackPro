using FinTrackPro.Application.Trading.Commands.ClosePosition;
using FinTrackPro.Application.Trading.Commands.CreateTrade;
using FinTrackPro.Application.Trading.Commands.DeleteTrade;
using FinTrackPro.Application.Trading.Commands.UpdateTrade;
using FinTrackPro.Application.Trading.DTOs;
using FinTrackPro.Application.Trading.Queries.GetTrades;
using FinTrackPro.Application.Trading.Queries.GetTradeSummary;
using FinTrackPro.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPro.API.Controllers;

[Authorize(Roles = UserRole.User)]
public class TradesController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetTradesQuery query)
        => Ok(await Mediator.Send(query));

    [HttpGet("summary")]
    public async Task<ActionResult<TradeSummaryDto>> GetSummary([FromQuery] GetTradeSummaryQuery query)
        => Ok(await Mediator.Send(query));

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

    [HttpPatch("{id:guid}/close")]
    public async Task<ActionResult<TradeDto>> Close(Guid id, ClosePositionCommand command)
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
