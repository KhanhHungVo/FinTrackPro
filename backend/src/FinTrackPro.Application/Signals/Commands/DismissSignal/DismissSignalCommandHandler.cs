using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Signals.Commands.DismissSignal;

public class DismissSignalCommandHandler(
    ISignalRepository signalRepository,
    ICurrentUser currentUser,
    IApplicationDbContext context) : IRequestHandler<DismissSignalCommand>
{
    public async Task Handle(DismissSignalCommand request, CancellationToken cancellationToken)
    {
        var signal = await signalRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Signal), request.Id);

        if (signal.UserId != currentUser.UserId)
            throw new AuthorizationException("You do not have permission to dismiss this signal.");

        signal.Dismiss();

        await context.SaveChangesAsync(cancellationToken);
    }
}
