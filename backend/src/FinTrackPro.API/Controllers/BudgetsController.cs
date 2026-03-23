using FinTrackPro.Application.Finance.Commands.CreateBudget;
using FinTrackPro.Application.Finance.Commands.DeleteBudget;
using FinTrackPro.Application.Finance.Commands.UpdateBudget;
using FinTrackPro.Application.Finance.Queries.GetBudgets;
using FinTrackPro.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPro.API.Controllers;

[Authorize(Roles = UserRole.User)]
public class BudgetsController : BaseApiController
{
    [HttpGet("{month}")]
    public async Task<ActionResult<IEnumerable<BudgetDto>>> GetByMonth(string month)
        => Ok(await Mediator.Send(new GetBudgetsQuery(month)));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateBudgetCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetByMonth), new { month = command.Month }, id);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBudgetRequest request)
    {
        await Mediator.Send(new UpdateBudgetCommand(id, request.LimitAmount));
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteBudgetCommand(id));
        return NoContent();
    }
}

public record UpdateBudgetRequest(decimal LimitAmount);
