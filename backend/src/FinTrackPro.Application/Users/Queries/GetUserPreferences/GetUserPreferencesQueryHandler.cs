using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Users.Queries.GetUserPreferences;

public class GetUserPreferencesQueryHandler(
    IUserRepository userRepository,
    ICurrentUser currentUser) : IRequestHandler<GetUserPreferencesQuery, UserPreferencesDto>
{
    public async Task<UserPreferencesDto> Handle(
        GetUserPreferencesQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        return new UserPreferencesDto(user.PreferredLanguage, user.PreferredCurrency);
    }
}
