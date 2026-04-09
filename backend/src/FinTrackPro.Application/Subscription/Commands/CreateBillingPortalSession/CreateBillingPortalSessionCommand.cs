using MediatR;

namespace FinTrackPro.Application.Subscription.Commands.CreateBillingPortalSession;

public record CreateBillingPortalSessionCommand(string ReturnUrl)
    : IRequest<BillingPortalSessionDto>;

public record BillingPortalSessionDto(string PortalUrl);
