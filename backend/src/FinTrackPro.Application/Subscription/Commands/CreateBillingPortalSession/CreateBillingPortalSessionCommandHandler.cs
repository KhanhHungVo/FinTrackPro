using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Subscription.Commands.CreateBillingPortalSession;

public class CreateBillingPortalSessionCommandHandler(
    ICurrentUser currentUser,
    IUserRepository userRepository,
    IPaymentGatewayService paymentGateway) : IRequestHandler<CreateBillingPortalSessionCommand, BillingPortalSessionDto>
{
    public async Task<BillingPortalSessionDto> Handle(
        CreateBillingPortalSessionCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        if (user.PaymentCustomerId is null)
            throw new DomainException("No billing account found. Please upgrade to Pro first.");

        var portalUrl = await paymentGateway.CreateBillingPortalSessionAsync(
            user.PaymentCustomerId, request.ReturnUrl, cancellationToken);

        return new BillingPortalSessionDto(portalUrl);
    }
}
