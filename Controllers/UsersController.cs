using Microsoft.AspNetCore.Mvc;
using MovieGraphApp.Services;

namespace MovieGraphApp.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly Neo4jService _neo4j;
    private readonly ILogger<UsersController> _logger;

    public UsersController(Neo4jService neo4j, ILogger<UsersController> logger)
    {
        _neo4j = neo4j;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        try
        {
            var users = await _neo4j.GetUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load users");
            return StatusCode(503, new { error = "Could not reach CognoDB. Please try again shortly." });
        }
    }

    [HttpGet("{id}/recommendations")]
    public async Task<IActionResult> GetRecommendations(string id)
    {
        try
        {
            var recs = await _neo4j.GetRecommendationsForUserAsync(id);
            return Ok(recs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compute recommendations for user {UserId}", id);
            return StatusCode(503, new { error = "Could not reach CognoDB. Please try again shortly." });
        }
    }
}
