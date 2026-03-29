using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Users.Commands.UpdateUserPreferences;

public class UpdateUserPreferencesCommandHandler(
    IApplicationDbContext context,
    IUserRepository userRepository,
    ICurrentUser currentUser) : IRequestHandler<UpdateUserPreferencesCommand>
{
    public async Task Handle(UpdateUserPreferencesCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        user.UpdatePreferences(request.Language, request.Currency);

        await context.SaveChangesAsync(cancellationToken);
    }
}
