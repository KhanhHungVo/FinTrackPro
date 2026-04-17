using FinTrackPro.Domain.Common;
using FinTrackPro.Domain.Exceptions;

namespace FinTrackPro.Domain.Entities;

public class WatchedSymbol : CreatedEntity
{
    public Guid UserId { get; private set; }
    public string Symbol { get; private set; } = string.Empty;


    private WatchedSymbol() { }

    public static WatchedSymbol Create(Guid userId, string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new DomainException("Symbol is required.");

        return new WatchedSymbol
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Symbol = symbol.Trim().ToUpperInvariant()
        };
    }
}
