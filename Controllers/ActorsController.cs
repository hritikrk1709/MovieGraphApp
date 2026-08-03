using Microsoft.AspNetCore.Mvc;
using MovieGraphApp.Services;

namespace MovieGraphApp.Controllers;

[ApiController]
[Route("api/actors")]
public class ActorsController : ControllerBase
{
    private readonly Neo4jService _neo4j;
    private readonly ILogger<ActorsController> _logger;

    public ActorsController(Neo4jService neo4j, ILogger<ActorsController> logger)
    {
        _neo4j = neo4j;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? search)
    {
        try
        {
            var actors = await _neo4j.SearchActorsAsync(search);
            return Ok(actors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search actors");
            return StatusCode(503, new { error = "Could not reach CognoDB. Please try again shortly." });
        }
    }

    [HttpGet("path")]
    public async Task<IActionResult> ShortestPath([FromQuery] string fromId, [FromQuery] string toId)
    {
        if (string.IsNullOrWhiteSpace(fromId) || string.IsNullOrWhiteSpace(toId))
        {
            return BadRequest(new { error = "fromId and toId are required." });
        }

        try
        {
            var result = await _neo4j.GetShortestPathBetweenActorsAsync(fromId, toId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compute shortest path {From} -> {To}", fromId, toId);
            return StatusCode(503, new { error = "Could not reach CognoDB. Please try again shortly." });
        }
    }
}
