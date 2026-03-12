using FinTrackPro.Domain.Entities;

namespace FinTrackPro.Application.Trading.Queries.GetWatchedSymbols;

public record WatchedSymbolDto(Guid Id, string Symbol, DateTime CreatedAt)
{
    public static explicit operator WatchedSymbolDto(WatchedSymbol w) => new(w.Id, w.Symbol, w.CreatedAt);
}
