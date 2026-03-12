using MediatR;

namespace FinTrackPro.Application.Trading.Commands.DeleteTrade;

public record DeleteTradeCommand(Guid Id) : IRequest;
