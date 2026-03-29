using MediatR;

namespace FinTrackPro.Application.Finance.Commands.CreateBudget;

public record CreateBudgetCommand(
    string Category,
    decimal LimitAmount,
    string Currency,
    string Month   // YYYY-MM
) : IRequest<Guid>;
