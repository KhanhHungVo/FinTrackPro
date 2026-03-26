using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Finance.Queries.GetBudgets;

public class GetBudgetsQueryHandler(
    IUserRepository userRepository,
    IBudgetRepository budgetRepository,
    ICurrentUser currentUser) : IRequestHandler<GetBudgetsQuery, IEnumerable<BudgetDto>>
{
    public async Task<IEnumerable<BudgetDto>> Handle(
        GetBudgetsQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        var budgets = await budgetRepository.GetByUserAndMonthAsync(user.Id, request.Month, cancellationToken);
        return budgets.Select(b => (BudgetDto)b);
    }
}
