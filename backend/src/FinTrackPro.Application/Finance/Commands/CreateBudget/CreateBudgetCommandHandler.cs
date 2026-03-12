using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Finance.Commands.CreateBudget;

public class CreateBudgetCommandHandler : IRequestHandler<CreateBudgetCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _userRepository;

    public CreateBudgetCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IUserRepository userRepository)
    {
        _context = context;
        _currentUser = currentUser;
        _userRepository = userRepository;
    }

    public async Task<Guid> Handle(CreateBudgetCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByKeycloakIdAsync(
            _currentUser.KeycloakUserId!, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), _currentUser.KeycloakUserId!);

        var budget = Budget.Create(user.Id, request.Category, request.LimitAmount, request.Month);

        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync(cancellationToken);

        return budget.Id;
    }
}
