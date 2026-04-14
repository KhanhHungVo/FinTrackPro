using FinTrackPro.Application.Finance.Commands.CreateTransaction;
using FinTrackPro.Application.Finance.Commands.DeleteTransaction;
using FinTrackPro.Application.Finance.Commands.UpdateTransaction;
using FinTrackPro.Application.Finance.DTOs;
using FinTrackPro.Application.Finance.Queries.GetTransactions;
using FinTrackPro.Application.Finance.Queries.GetTransactionSummary;
using FinTrackPro.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPro.API.Controllers;

[Authorize(Roles = UserRole.User)]
public class TransactionsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetTransactionsQuery query)
        => Ok(await Mediator.Send(query));

    [HttpGet("summary")]
    public async Task<ActionResult<TransactionSummaryDto>> GetSummary([FromQuery] GetTransactionSummaryQuery query)
        => Ok(await Mediator.Send(query));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateTransactionCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), new { }, id);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTransactionCommand command)
    {
        await Mediator.Send(command with { Id = id });
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteTransactionCommand(id));
        return NoContent();
    }
}
