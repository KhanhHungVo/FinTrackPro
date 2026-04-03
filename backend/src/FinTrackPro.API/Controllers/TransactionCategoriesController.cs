using FinTrackPro.Application.TransactionCategories.Commands.CreateTransactionCategory;
using FinTrackPro.Application.TransactionCategories.Commands.DeleteTransactionCategory;
using FinTrackPro.Application.TransactionCategories.Commands.UpdateTransactionCategory;
using FinTrackPro.Application.TransactionCategories.Queries.GetTransactionCategories;
using FinTrackPro.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPro.API.Controllers;

[Route("api/transaction-categories")]
[Authorize(Roles = Domain.Constants.UserRole.User)]
public class TransactionCategoriesController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TransactionCategoryDto>>> GetAll([FromQuery] TransactionType? type)
        => Ok(await Mediator.Send(new GetTransactionCategoriesQuery(type)));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateTransactionCategoryCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), new { }, id);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTransactionCategoryRequest request)
    {
        await Mediator.Send(new UpdateTransactionCategoryCommand(id, request.LabelEn, request.LabelVi, request.Icon));
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteTransactionCategoryCommand(id));
        return NoContent();
    }
}

public record UpdateTransactionCategoryRequest(string LabelEn, string LabelVi, string Icon);
