using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPro.API.Controllers;

[Authorize(Roles = UserRole.User)]
[ApiController]
[Route("api/[controller]")]
public class MarketController : ControllerBase
{
    private readonly IFearGreedService _fearGreedService;
    private readonly ICoinGeckoService _coinGeckoService;

    public MarketController(IFearGreedService fearGreedService, ICoinGeckoService coinGeckoService)
    {
        _fearGreedService = fearGreedService;
        _coinGeckoService = coinGeckoService;
    }

    [HttpGet("fear-greed")]
    public async Task<IActionResult> GetFearGreed(CancellationToken cancellationToken)
        => Ok(await _fearGreedService.GetLatestAsync(cancellationToken));

    [HttpGet("trending")]
    public async Task<IActionResult> GetTrending(CancellationToken cancellationToken)
        => Ok(await _coinGeckoService.GetTrendingCoinsAsync(cancellationToken));
}
