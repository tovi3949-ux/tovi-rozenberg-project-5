using CVSite.Services;
using Microsoft.AspNetCore.Mvc;

namespace CVSite.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PortfolioController : ControllerBase
{
    private readonly IGitHubService _gitHubService;

    public PortfolioController(IGitHubService gitHubService)
    {
        _gitHubService = gitHubService;
    }

    [HttpGet]
    public async Task<ActionResult<List<PortfolioRepositoryDto>>> GetPortfolio()
    {
        try
        {
            var result = await _gitHubService.GetPortfolioAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<RepositoryDto>>> SearchRepositories(
        [FromQuery] string? name,
        [FromQuery] string? language,
        [FromQuery] string? username)
    {
        try
        {
            var result = await _gitHubService.SearchRepositoriesAsync(name, language, username);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
