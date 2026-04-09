using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Finance.Queries.GetTransactions;

public class GetTransactionsQueryHandler(
    IUserRepository userRepository,
    ITransactionRepository transactionRepository,
    ISubscriptionLimitService subscriptionLimitService,
    ICurrentUser currentUser) : IRequestHandler<GetTransactionsQuery, IEnumerable<TransactionDto>>
{
    public async Task<IEnumerable<TransactionDto>> Handle(
        GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        // Enforce history access when a specific month is requested and it can be parsed to a date.
        if (!string.IsNullOrWhiteSpace(request.Month) &&
            DateTime.TryParseExact(request.Month, "yyyy-MM",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var fromDate))
        {
            await subscriptionLimitService.EnforceTransactionHistoryAccessAsync(
                user, fromDate, cancellationToken);
        }

        var transactions = await transactionRepository.GetByUserAsync(user.Id, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Month))
            transactions = transactions.Where(t => t.BudgetMonth == request.Month);

        return transactions
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => (TransactionDto)t);
    }
}
