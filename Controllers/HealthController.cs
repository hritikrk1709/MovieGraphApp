using Microsoft.AspNetCore.Mvc;
using MovieGraphApp.Services;

namespace MovieGraphApp.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly Neo4jService _neo4j;

    public HealthController(Neo4jService neo4j)
    {
        _neo4j = neo4j;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var connected = await _neo4j.CanConnectAsync();
        return Ok(new { status = connected ? "ok" : "degraded", cognodb = connected });
    }
}
