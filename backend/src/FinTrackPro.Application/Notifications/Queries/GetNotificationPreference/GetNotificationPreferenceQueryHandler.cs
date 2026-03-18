using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Notifications.Queries.GetNotificationPreference;

public class GetNotificationPreferenceQueryHandler
    : IRequestHandler<GetNotificationPreferenceQuery, NotificationPreferenceDto?>
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationPreferenceRepository _preferenceRepository;
    private readonly ICurrentUserService _currentUser;

    public GetNotificationPreferenceQueryHandler(
        IUserRepository userRepository,
        INotificationPreferenceRepository preferenceRepository,
        ICurrentUserService currentUser)
    {
        _userRepository = userRepository;
        _preferenceRepository = preferenceRepository;
        _currentUser = currentUser;
    }

    public async Task<NotificationPreferenceDto?> Handle(
        GetNotificationPreferenceQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByExternalIdAsync(
            _currentUser.ExternalUserId!, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), _currentUser.ExternalUserId!);

        var pref = await _preferenceRepository.GetByUserAsync(user.Id, cancellationToken);
        return pref is null ? null : (NotificationPreferenceDto)pref;
    }
}
