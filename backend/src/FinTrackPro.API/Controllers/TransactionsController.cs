using FinTrackPro.Application.Finance.Commands.CreateTransaction;
using FinTrackPro.Application.Finance.Commands.DeleteTransaction;
using FinTrackPro.Application.Finance.Queries.GetTransactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPro.API.Controllers;

[Authorize]
public class TransactionsController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetAll([FromQuery] string? month)
        => Ok(await Mediator.Send(new GetTransactionsQuery(month)));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateTransactionCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), new { }, id);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteTransactionCommand(id));
        return NoContent();
    }
}
