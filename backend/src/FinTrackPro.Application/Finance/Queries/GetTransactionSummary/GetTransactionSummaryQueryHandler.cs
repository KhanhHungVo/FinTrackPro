using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Finance.DTOs;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinTrackPro.Application.Finance.Queries.GetTransactionSummary;

public class GetTransactionSummaryQueryHandler(
    IUserRepository userRepository,
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<GetTransactionSummaryQuery, TransactionSummaryDto>
{
    public async Task<TransactionSummaryDto> Handle(
        GetTransactionSummaryQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        var q = context.Transactions.Where(t => t.UserId == user.Id);

        if (!string.IsNullOrWhiteSpace(request.Month))
            q = q.Where(t => t.BudgetMonth == request.Month);

        if (!string.IsNullOrWhiteSpace(request.Type) &&
            Enum.TryParse<TransactionType>(request.Type, ignoreCase: true, out var parsedType))
            q = q.Where(t => t.Type == parsedType);

        if (request.CategoryId.HasValue)
            q = q.Where(t => t.CategoryId == request.CategoryId.Value);

        var preferred = string.IsNullOrWhiteSpace(request.PreferredCurrency) ? "USD" : request.PreferredCurrency;
        var preferredRate = request.PreferredRate == 0 ? 1m : request.PreferredRate;

        // Mirror convertAmount.ts short-circuit: same currency → use amount directly (no USD round-trip)
        // Otherwise → normalize to USD then multiply by preferredRate
        var totalIncome = await q
            .Where(t => t.Type == TransactionType.Income)
            .SumAsync(t => t.Currency == preferred
                ? t.Amount
                : t.Amount / t.RateToUsd * preferredRate, cancellationToken);

        var totalExpense = await q
            .Where(t => t.Type == TransactionType.Expense)
            .SumAsync(t => t.Currency == preferred
                ? t.Amount
                : t.Amount / t.RateToUsd * preferredRate, cancellationToken);

        return new TransactionSummaryDto(totalIncome, totalExpense, totalIncome - totalExpense);
    }
}
