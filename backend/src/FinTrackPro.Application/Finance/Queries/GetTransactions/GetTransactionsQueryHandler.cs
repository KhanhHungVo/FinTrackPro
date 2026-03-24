using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Finance.Queries.GetTransactions;

public class GetTransactionsQueryHandler(
    IUserRepository userRepository,
    ITransactionRepository transactionRepository,
    ICurrentUserService currentUser) : IRequestHandler<GetTransactionsQuery, IEnumerable<TransactionDto>>
{
    public async Task<IEnumerable<TransactionDto>> Handle(
        GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByExternalIdAsync(
            currentUser.ExternalUserId!, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.ExternalUserId!);

        var transactions = await transactionRepository.GetByUserAsync(user.Id, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Month))
            transactions = transactions.Where(t => t.BudgetMonth == request.Month);

        return transactions
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => (TransactionDto)t);
    }
}
