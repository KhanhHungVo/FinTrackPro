using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Finance.Commands.CreateTransaction;

public class CreateTransactionCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IUserRepository userRepository) : IRequestHandler<CreateTransactionCommand, Guid>
{
    public async Task<Guid> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByExternalIdAsync(
            currentUser.ExternalUserId!, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.ExternalUserId!);

        var transaction = Transaction.Create(
            user.Id, request.Type, request.Amount,
            request.Category, request.Note, request.BudgetMonth);

        context.Transactions.Add(transaction);
        await context.SaveChangesAsync(cancellationToken);

        return transaction.Id;
    }
}
