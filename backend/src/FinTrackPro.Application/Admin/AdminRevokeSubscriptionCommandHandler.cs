using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Admin;

public class AdminRevokeSubscriptionCommandHandler(
    IUserRepository userRepository,
    IApplicationDbContext context)
    : IRequestHandler<AdminRevokeSubscriptionCommand>
{
    public async Task Handle(AdminRevokeSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), request.UserId);

        user.CancelSubscription();
        await context.SaveChangesAsync(cancellationToken);
    }
}
