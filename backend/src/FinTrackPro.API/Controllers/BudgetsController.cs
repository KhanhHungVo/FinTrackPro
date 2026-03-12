using FinTrackPro.Application.Finance.Commands.CreateBudget;
using FinTrackPro.Application.Finance.Queries.GetBudgets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPro.API.Controllers;

[Authorize]
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
}
