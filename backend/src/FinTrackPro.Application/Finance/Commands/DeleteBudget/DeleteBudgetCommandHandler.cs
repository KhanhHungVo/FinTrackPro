using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Finance.Commands.DeleteBudget;

public class DeleteBudgetCommandHandler : IRequestHandler<DeleteBudgetCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IBudgetRepository _budgetRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _userRepository;

    public DeleteBudgetCommandHandler(
        IApplicationDbContext context,
        IBudgetRepository budgetRepository,
        ICurrentUserService currentUser,
        IUserRepository userRepository)
    {
        _context = context;
        _budgetRepository = budgetRepository;
        _currentUser = currentUser;
        _userRepository = userRepository;
    }

    public async Task Handle(DeleteBudgetCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByExternalIdAsync(
            _currentUser.ExternalUserId!, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), _currentUser.ExternalUserId!);

        var budget = await _budgetRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Budget), request.Id);

        if (budget.UserId != user.Id)
            throw new DomainException("You are not authorized to delete this budget.");

        _budgetRepository.Remove(budget);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
