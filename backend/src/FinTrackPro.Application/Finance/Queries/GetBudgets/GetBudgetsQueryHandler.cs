using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Finance.Queries.GetBudgets;

public class GetBudgetsQueryHandler : IRequestHandler<GetBudgetsQuery, IEnumerable<BudgetDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IBudgetRepository _budgetRepository;
    private readonly ICurrentUserService _currentUser;

    public GetBudgetsQueryHandler(
        IUserRepository userRepository,
        IBudgetRepository budgetRepository,
        ICurrentUserService currentUser)
    {
        _userRepository = userRepository;
        _budgetRepository = budgetRepository;
        _currentUser = currentUser;
    }

    public async Task<IEnumerable<BudgetDto>> Handle(
        GetBudgetsQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByKeycloakIdAsync(
            _currentUser.KeycloakUserId!, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), _currentUser.KeycloakUserId!);

        var budgets = await _budgetRepository.GetByUserAndMonthAsync(user.Id, request.Month, cancellationToken);
        return budgets.Select(b => (BudgetDto)b);
    }
}
