using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.TransactionCategories.Commands.CreateTransactionCategory;

public class CreateTransactionCategoryCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IUserRepository userRepository,
    ITransactionCategoryRepository categoryRepository)
    : IRequestHandler<CreateTransactionCategoryCommand, Guid>
{
    public async Task<Guid> Handle(CreateTransactionCategoryCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        var slugExists = await categoryRepository.SlugExistsForUserAsync(user.Id, request.Slug, cancellationToken);
        if (slugExists)
            throw new ConflictException($"A category with slug '{request.Slug}' already exists.");

        var category = TransactionCategory.Create(
            user.Id, request.Type, request.Slug,
            request.LabelEn, request.LabelVi, request.Icon);

        categoryRepository.Add(category);
        await context.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}
