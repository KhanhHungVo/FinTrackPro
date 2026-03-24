using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPro.API.Controllers;

[Authorize(Roles = UserRole.User)]
[ApiController]
[Route("api/[controller]")]
public class MarketController(
    IFearGreedService fearGreedService,
    ICoinGeckoService coinGeckoService) : ControllerBase
{
    [HttpGet("fear-greed")]
    public async Task<IActionResult> GetFearGreed(CancellationToken cancellationToken)
        => Ok(await fearGreedService.GetLatestAsync(cancellationToken));

    [HttpGet("trending")]
    public async Task<IActionResult> GetTrending(CancellationToken cancellationToken)
        => Ok(await coinGeckoService.GetTrendingCoinsAsync(cancellationToken));
}
