using Microsoft.AspNetCore.Mvc;
using MovieGraphApp.Services;

namespace MovieGraphApp.Controllers;

[ApiController]
[Route("api/movies")]
public class MoviesController : ControllerBase
{
    private readonly Neo4jService _neo4j;
    private readonly ILogger<MoviesController> _logger;

    public MoviesController(Neo4jService neo4j, ILogger<MoviesController> logger)
    {
        _neo4j = neo4j;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? search)
    {
        try
        {
            var movies = await _neo4j.SearchMoviesAsync(search);
            return Ok(movies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search movies");
            return StatusCode(503, new { error = "Could not reach CognoDB. Please try again shortly." });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetail(string id)
    {
        try
        {
            var movie = await _neo4j.GetMovieDetailAsync(id);
            return movie is null ? NotFound() : Ok(movie);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load movie {MovieId}", id);
            return StatusCode(503, new { error = "Could not reach CognoDB. Please try again shortly." });
        }
    }

    [HttpGet("{id}/graph")]
    public async Task<IActionResult> GetNeighborhood(string id)
    {
        try
        {
            var graph = await _neo4j.GetMovieNeighborhoodAsync(id);
            return Ok(graph);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load neighborhood for movie {MovieId}", id);
            return StatusCode(503, new { error = "Could not reach CognoDB. Please try again shortly." });
        }
    }
}
