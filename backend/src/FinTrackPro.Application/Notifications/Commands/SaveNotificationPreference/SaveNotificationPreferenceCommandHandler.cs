using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Notifications.Commands.SaveNotificationPreference;

public class SaveNotificationPreferenceCommandHandler : IRequestHandler<SaveNotificationPreferenceCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _userRepository;
    private readonly INotificationPreferenceRepository _preferenceRepository;

    public SaveNotificationPreferenceCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IUserRepository userRepository,
        INotificationPreferenceRepository preferenceRepository)
    {
        _context = context;
        _currentUser = currentUser;
        _userRepository = userRepository;
        _preferenceRepository = preferenceRepository;
    }

    public async Task Handle(SaveNotificationPreferenceCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByKeycloakIdAsync(
            _currentUser.KeycloakUserId!, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), _currentUser.KeycloakUserId!);

        var existing = await _preferenceRepository.GetByUserAsync(user.Id, cancellationToken);
        if (existing is null)
        {
            var preference = NotificationPreference.CreateTelegram(user.Id, request.TelegramChatId);
            if (!request.IsEnabled) preference.Update(request.TelegramChatId, false);
            _preferenceRepository.Add(preference);
        }
        else
        {
            existing.Update(request.TelegramChatId, request.IsEnabled);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
