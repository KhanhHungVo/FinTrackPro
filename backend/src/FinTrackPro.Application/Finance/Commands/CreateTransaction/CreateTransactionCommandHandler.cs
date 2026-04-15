using FinTrackPro.Application.Common.Extensions;
using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Finance.Commands.CreateTransaction;

public class CreateTransactionCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IUserRepository userRepository,
    ITransactionCategoryRepository categoryRepository,
    ITransactionRepository transactionRepository,
    ISubscriptionLimitService subscriptionLimitService,
    IExchangeRateService exchangeRateService) : IRequestHandler<CreateTransactionCommand, Guid>
{
    public async Task<Guid> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        var category = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(TransactionCategory), request.CategoryId);

        if (category.UserId != null && category.UserId != user.Id)
            throw new AuthorizationException("Category does not belong to this user.");

        var rateToUsd = await exchangeRateService.GetRateForCurrencyAsync(request.Currency, cancellationToken);

        await using var tx = await context.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.BudgetMonth))
            await subscriptionLimitService.EnforceMonthlyTransactionLimitAsync(
                user, transactionRepository, request.BudgetMonth, cancellationToken);

        var transaction = Transaction.Create(
            user.Id, request.Type, request.Amount,
            request.Currency, rateToUsd,
            category.Slug, request.Note, request.BudgetMonth,
            categoryId: request.CategoryId);

        context.Transactions.Add(transaction);
        await context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return transaction.Id;
    }
}
