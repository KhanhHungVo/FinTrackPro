using MediatR;

namespace FinTrackPro.Application.Users.Queries.GetUserPreferences;

public record GetUserPreferencesQuery : IRequest<UserPreferencesDto>;
