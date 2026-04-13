using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Common.Models;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Finance.Queries.GetTransactions;

public class GetTransactionsQueryHandler(
    IUserRepository userRepository,
    ITransactionRepository transactionRepository,
    ISubscriptionLimitService subscriptionLimitService,
    ICurrentUser currentUser) : IRequestHandler<GetTransactionsQuery, PagedResult<TransactionDto>>
{
    private const int MaxPageSize = 100;

    public async Task<PagedResult<TransactionDto>> Handle(
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

        var pageSize = Math.Min(request.PageSize, MaxPageSize);
        var pageQuery = new TransactionPageQuery(
            request.Page, pageSize, request.Search,
            request.Month, request.Type, request.CategoryId,
            request.SortBy, request.SortDir);

        var (items, totalCount) = await transactionRepository.GetPagedAsync(user.Id, pageQuery, cancellationToken);

        return new PagedResult<TransactionDto>(
            items.Select(t => (TransactionDto)t).ToList(),
            request.Page,
            pageSize,
            totalCount);
    }
}
