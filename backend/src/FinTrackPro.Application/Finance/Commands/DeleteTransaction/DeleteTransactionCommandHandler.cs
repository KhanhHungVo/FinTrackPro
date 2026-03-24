using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Finance.Commands.DeleteTransaction;

public class DeleteTransactionCommandHandler(
    IApplicationDbContext context,
    ITransactionRepository transactionRepository,
    ICurrentUserService currentUser,
    IUserRepository userRepository) : IRequestHandler<DeleteTransactionCommand>
{
    public async Task Handle(DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByExternalIdAsync(
            currentUser.ExternalUserId!, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.ExternalUserId!);

        var transaction = await transactionRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Transaction), request.Id);

        if (transaction.UserId != user.Id)
            throw new AuthorizationException("You are not authorized to delete this transaction.");

        transactionRepository.Remove(transaction);
        await context.SaveChangesAsync(cancellationToken);
    }
}
