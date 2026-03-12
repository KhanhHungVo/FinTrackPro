using MediatR;

namespace FinTrackPro.Application.Trading.Commands.AddWatchedSymbol;

public record AddWatchedSymbolCommand(string Symbol) : IRequest<Guid>;
