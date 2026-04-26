using MediatR;

namespace FinTrackPro.Application.Admin;

public record AdminRevokeSubscriptionCommand(Guid UserId) : IRequest;
