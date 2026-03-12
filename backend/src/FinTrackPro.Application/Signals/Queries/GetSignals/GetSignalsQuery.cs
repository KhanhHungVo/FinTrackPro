using MediatR;

namespace FinTrackPro.Application.Signals.Queries.GetSignals;

public record GetSignalsQuery(int Count = 20) : IRequest<IEnumerable<SignalDto>>;
