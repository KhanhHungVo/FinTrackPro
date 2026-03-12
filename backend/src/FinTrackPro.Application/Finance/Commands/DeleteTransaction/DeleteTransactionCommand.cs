using MediatR;

namespace FinTrackPro.Application.Finance.Commands.DeleteTransaction;

public record DeleteTransactionCommand(Guid Id) : IRequest;
