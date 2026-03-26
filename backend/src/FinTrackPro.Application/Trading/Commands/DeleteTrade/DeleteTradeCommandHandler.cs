using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Trading.Commands.DeleteTrade;

public class DeleteTradeCommandHandler(
    IApplicationDbContext context,
    ITradeRepository tradeRepository,
    ICurrentUser currentUser,
    IUserRepository userRepository) : IRequestHandler<DeleteTradeCommand>
{
    public async Task Handle(DeleteTradeCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        var trade = await tradeRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Trade), request.Id);

        if (trade.UserId != user.Id)
            throw new AuthorizationException("You are not authorized to delete this trade.");

        tradeRepository.Remove(trade);
        await context.SaveChangesAsync(cancellationToken);
    }
}
