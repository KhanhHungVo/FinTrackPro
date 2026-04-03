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

        var transaction = Transaction.Create(
            user.Id, request.Type, request.Amount,
            request.Currency, rateToUsd,
            category.Slug, request.Note, request.BudgetMonth,
            categoryId: request.CategoryId);

        context.Transactions.Add(transaction);
        await context.SaveChangesAsync(cancellationToken);

        return transaction.Id;
    }
}
