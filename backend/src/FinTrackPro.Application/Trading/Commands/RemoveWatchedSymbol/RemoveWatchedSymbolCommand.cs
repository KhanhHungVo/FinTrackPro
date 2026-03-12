using MediatR;

namespace FinTrackPro.Application.Trading.Commands.RemoveWatchedSymbol;

public record RemoveWatchedSymbolCommand(Guid Id) : IRequest;
