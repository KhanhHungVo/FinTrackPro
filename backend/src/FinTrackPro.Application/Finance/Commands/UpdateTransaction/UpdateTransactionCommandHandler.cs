using FinTrackPro.Application.Common.Extensions;
using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Finance.Commands.UpdateTransaction;

public class UpdateTransactionCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IUserRepository userRepository,
    ITransactionRepository transactionRepository,
    IExchangeRateService exchangeRateService,
    ITransactionCategoryRepository categoryRepository) : IRequestHandler<UpdateTransactionCommand>
{
    public async Task Handle(UpdateTransactionCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        var transaction = await transactionRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Transaction), request.Id);

        if (transaction.UserId != user.Id)
            throw new AuthorizationException("You are not authorized to update this transaction.");

        if (request.CategoryId.HasValue)
        {
            var category = await categoryRepository.GetByIdAsync(request.CategoryId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(TransactionCategory), request.CategoryId.Value);

            if (category.UserId != null && category.UserId != user.Id)
                throw new AuthorizationException("Category does not belong to this user.");
        }

        var rateToUsd = await exchangeRateService.GetRateForCurrencyAsync(request.Currency, cancellationToken);

        transaction.Update(request.Type, request.Amount, request.Currency, rateToUsd,
            request.Category, request.Note, request.CategoryId);

        await context.SaveChangesAsync(cancellationToken);
    }
}
