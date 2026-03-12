using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Trading.Commands.DeleteTrade;

public class DeleteTradeCommandHandler : IRequestHandler<DeleteTradeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITradeRepository _tradeRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _userRepository;

    public DeleteTradeCommandHandler(
        IApplicationDbContext context,
        ITradeRepository tradeRepository,
        ICurrentUserService currentUser,
        IUserRepository userRepository)
    {
        _context = context;
        _tradeRepository = tradeRepository;
        _currentUser = currentUser;
        _userRepository = userRepository;
    }

    public async Task Handle(DeleteTradeCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByKeycloakIdAsync(
            _currentUser.KeycloakUserId!, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), _currentUser.KeycloakUserId!);

        var trade = await _tradeRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Trade), request.Id);

        if (trade.UserId != user.Id)
            throw new DomainException("You are not authorized to delete this trade.");

        _tradeRepository.Remove(trade);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
