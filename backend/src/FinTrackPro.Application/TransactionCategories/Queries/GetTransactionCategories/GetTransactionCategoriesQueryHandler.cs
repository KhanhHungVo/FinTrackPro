using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.TransactionCategories.Queries.GetTransactionCategories;

public class GetTransactionCategoriesQueryHandler(
    ICurrentUser currentUser,
    IUserRepository userRepository,
    ITransactionCategoryRepository categoryRepository)
    : IRequestHandler<GetTransactionCategoriesQuery, IEnumerable<TransactionCategoryDto>>
{
    public async Task<IEnumerable<TransactionCategoryDto>> Handle(
        GetTransactionCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        var categories = await categoryRepository.GetByUserAsync(user.Id, request.Type, cancellationToken);

        return categories.Select(c => (TransactionCategoryDto)c);
    }
}
