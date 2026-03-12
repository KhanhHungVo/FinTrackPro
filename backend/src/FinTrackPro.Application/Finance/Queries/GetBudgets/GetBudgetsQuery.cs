using MediatR;

namespace FinTrackPro.Application.Finance.Queries.GetBudgets;

public record GetBudgetsQuery(string Month) : IRequest<IEnumerable<BudgetDto>>;
