using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Market.Queries.GetExchangeRates;
using FinTrackPro.Application.Market.Queries.GetMarketCapCoins;
using FinTrackPro.Application.Market.Queries.GetTrendingCoins;
using FinTrackPro.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPro.API.Controllers;

[Authorize(Roles = UserRole.User)]
[ApiController]
[Route("api/[controller]")]
public class MarketController(IFearGreedService fearGreedService) : BaseApiController
{
    [HttpGet("fear-greed")]
    public async Task<IActionResult> GetFearGreed(CancellationToken cancellationToken)
        => Ok(await fearGreedService.GetLatestAsync(cancellationToken));

    [HttpGet("trending")]
    public async Task<IActionResult> GetTrending(CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetTrendingCoinsQuery(), cancellationToken));

    [HttpGet("marketcap")]
    public async Task<IActionResult> GetMarketCap(CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetMarketCapCoinsQuery(), cancellationToken));

    [HttpGet("rates")]
    public async Task<ActionResult<Dictionary<string, decimal>>> GetRates(
        [FromQuery] string currencies,
        CancellationToken cancellationToken)
    {
        var codes = (currencies ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (codes.Length == 0)
            return BadRequest("At least one currency code is required.");

        var result = await Mediator.Send(new GetExchangeRatesQuery(codes), cancellationToken);
        return Ok(result);
    }
}
