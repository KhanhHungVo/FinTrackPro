namespace FinTrackPro.Application.Common.Models;

public record KlineDto(
    DateTime OpenTime,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Volume
);
