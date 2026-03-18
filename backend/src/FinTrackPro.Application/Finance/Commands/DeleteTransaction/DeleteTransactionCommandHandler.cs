using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Finance.Commands.DeleteTransaction;

public class DeleteTransactionCommandHandler : IRequestHandler<DeleteTransactionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _userRepository;

    public DeleteTransactionCommandHandler(
        IApplicationDbContext context,
        ITransactionRepository transactionRepository,
        ICurrentUserService currentUser,
        IUserRepository userRepository)
    {
        _context = context;
        _transactionRepository = transactionRepository;
        _currentUser = currentUser;
        _userRepository = userRepository;
    }

    public async Task Handle(DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByExternalIdAsync(
            _currentUser.ExternalUserId!, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), _currentUser.ExternalUserId!);

        var transaction = await _transactionRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Transaction), request.Id);

        if (transaction.UserId != user.Id)
            throw new DomainException("You are not authorized to delete this transaction.");

        _transactionRepository.Remove(transaction);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
