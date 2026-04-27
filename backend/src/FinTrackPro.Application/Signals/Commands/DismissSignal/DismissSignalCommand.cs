using MediatR;

namespace FinTrackPro.Application.Signals.Commands.DismissSignal;

public record DismissSignalCommand(Guid Id) : IRequest;
