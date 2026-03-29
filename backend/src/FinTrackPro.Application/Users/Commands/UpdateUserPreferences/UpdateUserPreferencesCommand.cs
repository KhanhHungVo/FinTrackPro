using MediatR;

namespace FinTrackPro.Application.Users.Commands.UpdateUserPreferences;

public record UpdateUserPreferencesCommand(string Language, string Currency) : IRequest;
