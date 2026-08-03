using MovieGraphApp.Models;
using Neo4j.Driver;

namespace MovieGraphApp.Services;

public class Neo4jService : IAsyncDisposable
{
    private readonly IDriver _driver;
    private readonly ILogger<Neo4jService> _logger;

    public Neo4jService(IConfiguration config, ILogger<Neo4jService> logger)
    {
        _logger = logger;

        // Read connection details from environment variables (never hardcode secrets).
        // CognoDB gives you: bolt+s://<instanceid>.databases.cognodb.cloud, user "cognodb", a generated password.
        var uri = config["Neo4j:Uri"] ?? throw new InvalidOperationException("Neo4j:Uri is not configured (set NEO4J__URI env var).");
        var user = config["Neo4j:User"] ?? throw new InvalidOperationException("Neo4j:User is not configured (set NEO4J__USER env var).");
        var password = config["Neo4j:Password"] ?? throw new InvalidOperationException("Neo4j:Password is not configured (set NEO4J__PASSWORD env var).");

        _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
    }

    public async Task<bool> CanConnectAsync()
    {
        try
        {
            await _driver.VerifyConnectivityAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CognoDB connectivity check failed");
            return false;
        }
    }

    // ---------- Schema / seeding ----------

    private void EnsureDriverConfigured()
    {
        if (_driver is null)
        {
            throw new InvalidOperationException("Neo4j is not configured. Set NEO4J__URI, NEO4J__USER, and NEO4J__PASSWORD before using the database.");
        }
    }

    public async Task EnsureConstraintsAsync()
    {
        EnsureDriverConfigured();
        await using var session = _driver.AsyncSession();
        var constraints = new[]
        {
            "CREATE CONSTRAINT movie_id IF NOT EXISTS FOR (m:Movie) REQUIRE m.id IS UNIQUE",
            "CREATE CONSTRAINT person_id IF NOT EXISTS FOR (p:Person) REQUIRE p.id IS UNIQUE",
            "CREATE CONSTRAINT genre_name IF NOT EXISTS FOR (g:Genre) REQUIRE g.name IS UNIQUE",
            "CREATE CONSTRAINT user_id IF NOT EXISTS FOR (u:User) REQUIRE u.id IS UNIQUE",
        };

        foreach (var c in constraints)
        {
            var cursor = await session.RunAsync(c);
            await cursor.ConsumeAsync();
        }
    }

    public async Task ClearDatabaseAsync()
    {
        EnsureDriverConfigured();
        await using var session = _driver.AsyncSession();
        var cursor = await session.RunAsync("MATCH (n) DETACH DELETE n");
        await cursor.ConsumeAsync();
    }

    public async Task RunWriteAsync(string cypher, IDictionary<string, object> parameters)
    {
        EnsureDriverConfigured();
        await using var session = _driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx =>
        {
            var cursor = await tx.RunAsync(cypher, parameters);
            await cursor.ConsumeAsync();
        });
    }

    // ---------- Movies ----------

    public async Task<List<MovieSummary>> SearchMoviesAsync(string? search, int limit = 30)
    {
        EnsureDriverConfigured();
        await using var session = _driver.AsyncSession();
        var cursor = await session.RunAsync(
            """
            MATCH (m:Movie)
            WHERE $search IS NULL OR toLower(m.title) CONTAINS toLower($search)
            OPTIONAL MATCH (m)-[:IN_GENRE]->(g:Genre)
            WITH m, collect(DISTINCT g.name) AS genres
            RETURN m.id AS id, m.title AS title, m.year AS year, m.posterUrl AS posterUrl, genres
            ORDER BY m.title
            LIMIT $limit
            """,
            new { search, limit });

        var records = await cursor.ToListAsync();
        return records.Select(r => new MovieSummary
        {
            Id = r["id"].As<string>(),
            Title = r["title"].As<string>(),
            Year = r["year"].As<int>(),
            PosterUrl = r["posterUrl"].As<string>(),
            Genres = r["genres"].As<List<object>>().Select(g => g.ToString()!).ToList()
        }).ToList();
    }

    public async Task<Movie?> GetMovieDetailAsync(string movieId)
    {
        EnsureDriverConfigured();
        await using var session = _driver.AsyncSession();
        var cursor = await session.RunAsync(
            """
            MATCH (m:Movie {id: $movieId})
            OPTIONAL MATCH (m)-[:IN_GENRE]->(g:Genre)
            OPTIONAL MATCH (actor:Person)-[r:ACTED_IN]->(m)
            OPTIONAL MATCH (director:Person)-[:DIRECTED]->(m)
            RETURN m,
                   collect(DISTINCT g.name) AS genres,
                   collect(DISTINCT {id: actor.id, name: actor.name, role: r.role}) AS cast,
                   collect(DISTINCT {id: director.id, name: director.name}) AS directors
            """,
            new { movieId });

        var records = await cursor.ToListAsync();
        if (records.Count == 0) return null;
        var record = records[0];
        var node = record["m"].As<INode>();

        return new Movie
        {
            Id = node.Properties["id"].As<string>(),
            Title = node.Properties["title"].As<string>(),
            Year = node.Properties["year"].As<int>(),
            Plot = node.Properties.GetValueOrDefault("plot", "").As<string>(),
            PosterUrl = node.Properties.GetValueOrDefault("posterUrl", "").As<string>(),
            Genres = record["genres"].As<List<object>>().Select(g => g.ToString()!).ToList(),
            Cast = record["cast"].As<List<object>>()
                .Select(o => (IReadOnlyDictionary<string, object>)o)
                .Where(d => d["id"] != null)
                .Select(d => new PersonSummary { Id = d["id"].As<string>(), Name = d["name"].As<string>(), Role = d["role"]?.As<string>() })
                .ToList(),
            Directors = record["directors"].As<List<object>>()
                .Select(o => (IReadOnlyDictionary<string, object>)o)
                .Where(d => d["id"] != null)
                .Select(d => new PersonSummary { Id = d["id"].As<string>(), Name = d["name"].As<string>() })
                .ToList(),
        };
    }

    /// <summary>
    /// Multi-hop (2-hop) neighborhood around a movie: its cast, director, genres, and
    /// "similar movies" reached via shared genre + shared actor - built specifically to
    /// feed the force-directed graph visualization on the frontend.
    /// </summary>
    public async Task<GraphResult> GetMovieNeighborhoodAsync(string movieId)
    {
        EnsureDriverConfigured();
        await using var session = _driver.AsyncSession();
        var cursor = await session.RunAsync(
            """
            MATCH (m:Movie {id: $movieId})
            OPTIONAL MATCH (m)-[:IN_GENRE]->(g:Genre)
            OPTIONAL MATCH (actor:Person)-[acted:ACTED_IN]->(m)
            OPTIONAL MATCH (director:Person)-[:DIRECTED]->(m)
            OPTIONAL MATCH (m)-[:IN_GENRE]->(:Genre)<-[:IN_GENRE]-(similar:Movie)
            WHERE similar <> m
            WITH m, g, actor, acted, director, similar
            LIMIT 60
            RETURN m, collect(DISTINCT g) AS genres, collect(DISTINCT actor) AS cast,
                   collect(DISTINCT director) AS directors, collect(DISTINCT similar) AS similarMovies
            """,
            new { movieId });

        var records = await cursor.ToListAsync();
        var result = new GraphResult();
        if (records.Count == 0) return result;
        var record = records[0];

        var movieNode = record["m"].As<INode>();
        AddMovieNode(result, movieNode);

        foreach (var g in record["genres"].As<List<object>>().Where(x => x != null).Cast<INode>())
        {
            result.Nodes.Add(new GraphNode { Id = "genre-" + g.Properties["name"], Label = g.Properties["name"].As<string>(), Group = "Genre" });
            result.Edges.Add(new GraphEdge { From = movieNode.Properties["id"].As<string>(), To = "genre-" + g.Properties["name"], Type = "IN_GENRE" });
        }

        foreach (var a in record["cast"].As<List<object>>().Where(x => x != null).Cast<INode>())
        {
            AddPersonNode(result, a);
            result.Edges.Add(new GraphEdge { From = a.Properties["id"].As<string>(), To = movieNode.Properties["id"].As<string>(), Type = "ACTED_IN" });
        }

        foreach (var d in record["directors"].As<List<object>>().Where(x => x != null).Cast<INode>())
        {
            AddPersonNode(result, d);
            result.Edges.Add(new GraphEdge { From = d.Properties["id"].As<string>(), To = movieNode.Properties["id"].As<string>(), Type = "DIRECTED" });
        }

        foreach (var sm in record["similarMovies"].As<List<object>>().Where(x => x != null).Cast<INode>())
        {
            AddMovieNode(result, sm);
            result.Edges.Add(new GraphEdge { From = movieNode.Properties["id"].As<string>(), To = sm.Properties["id"].As<string>(), Type = "SIMILAR_GENRE" });
        }

        return result;
    }

    // ---------- Users & recommendations ----------

    public async Task<List<UserSummary>> GetUsersAsync()
    {
        EnsureDriverConfigured();
        await using var session = _driver.AsyncSession();
        var cursor = await session.RunAsync("MATCH (u:User) RETURN u.id AS id, u.name AS name ORDER BY u.name");
        var records = await cursor.ToListAsync();
        return records.Select(r => new UserSummary { Id = r["id"].As<string>(), Name = r["name"].As<string>() }).ToList();
    }

    /// <summary>
    /// The centerpiece query: 3-hop collaborative filtering.
    /// User -> RATED -> Movie <- RATED <- OtherUser -> RATED -> Recommendation
    /// "People who liked what you liked also liked these movies you haven't seen."
    /// This is the query a relational database would find genuinely awkward: it requires
    /// a self-join through a bridge table three times over, with a live exclusion filter,
    /// and gets dramatically slower as the ratings table grows. In a graph it's one traversal.
    /// </summary>
    public async Task<List<Recommendation>> GetRecommendationsForUserAsync(string userId, int limit = 10)
    {
        EnsureDriverConfigured();
        await using var session = _driver.AsyncSession();
        var cursor = await session.RunAsync(
            """
            MATCH (u:User {id: $userId})-[r1:RATED]->(m:Movie)<-[r2:RATED]-(other:User)-[r3:RATED]->(rec:Movie)
            WHERE u <> other
              AND r1.rating >= 4
              AND r2.rating >= 4
              AND r3.rating >= 4
              AND NOT (u)-[:RATED]->(rec)
            OPTIONAL MATCH (rec)-[:IN_GENRE]->(g:Genre)
            WITH rec, count(DISTINCT other) AS score, collect(DISTINCT other.name)[0..3] AS sampleUsers, collect(DISTINCT g.name) AS genres
            RETURN rec.id AS id, rec.title AS title, rec.year AS year, rec.posterUrl AS posterUrl,
                   genres, score, sampleUsers
            ORDER BY score DESC
            LIMIT $limit
            """,
            new { userId, limit });

        var records = await cursor.ToListAsync();
        return records.Select(r => new Recommendation
        {
            Movie = new MovieSummary
            {
                Id = r["id"].As<string>(),
                Title = r["title"].As<string>(),
                Year = r["year"].As<int>(),
                PosterUrl = r["posterUrl"].As<string>(),
                Genres = r["genres"].As<List<object>>().Select(g => g.ToString()!).ToList()
            },
            Score = r["score"].As<int>(),
            BecauseUsersAlsoLiked = r["sampleUsers"].As<List<object>>().Select(s => s.ToString()!).ToList()
        }).ToList();
    }

    // ---------- Actors ----------

    public async Task<List<PersonSummary>> SearchActorsAsync(string? search, int limit = 30)
    {
        EnsureDriverConfigured();
        await using var session = _driver.AsyncSession();
        var cursor = await session.RunAsync(
            """
            MATCH (p:Person)
            WHERE $search IS NULL OR toLower(p.name) CONTAINS toLower($search)
            RETURN p.id AS id, p.name AS name
            ORDER BY p.name
            LIMIT $limit
            """,
            new { search, limit });
        var records = await cursor.ToListAsync();
        return records.Select(r => new PersonSummary { Id = r["id"].As<string>(), Name = r["name"].As<string>() }).ToList();
    }

    /// <summary>
    /// "Six degrees of Kevin Bacon" - shortest path between two actors, hopping through
    /// the movies they share a cast with. This is the other classic "relational databases
    /// hate this" query: variable-length shortest-path search has no clean fixed-join SQL
    /// equivalent and requires recursive CTEs that don't scale.
    /// </summary>
    public async Task<ActorPathResult> GetShortestPathBetweenActorsAsync(string fromId, string toId)
    {
        EnsureDriverConfigured();
        await using var session = _driver.AsyncSession();
        var cursor = await session.RunAsync(
            """
            MATCH (a:Person {id: $fromId}), (b:Person {id: $toId})
            MATCH path = shortestPath((a)-[:ACTED_IN*..10]-(b))
            RETURN path
            """,
            new { fromId, toId });

        var records = await cursor.ToListAsync();
        if (records.Count == 0)
        {
            return new ActorPathResult { Found = false };
        }

        var path = records[0]["path"].As<IPath>();
        var result = new GraphResult();

        foreach (var node in path.Nodes)
        {
            if (node.Labels.Contains("Person")) AddPersonNode(result, node);
            else if (node.Labels.Contains("Movie")) AddMovieNode(result, node);
        }

        foreach (var rel in path.Relationships)
        {
            result.Edges.Add(new GraphEdge { From = rel.StartNodeElementId, To = rel.EndNodeElementId, Type = rel.Type });
        }

        // path.Relationships is keyed by element id above for correctness with the driver's
        // internal ids; remap to our own "id" property based ids used elsewhere in the UI.
        var idByElementId = new Dictionary<string, string>();
        foreach (var node in path.Nodes)
        {
            idByElementId[node.ElementId] = node.Properties["id"].As<string>();
        }
        foreach (var edge in result.Edges)
        {
            edge.From = idByElementId.GetValueOrDefault(edge.From, edge.From);
            edge.To = idByElementId.GetValueOrDefault(edge.To, edge.To);
        }

        return new ActorPathResult
        {
            Found = true,
            Hops = path.Relationships.Count(),
            Graph = result
        };
    }

    // ---------- helpers ----------

    private static void AddMovieNode(GraphResult result, INode node)
    {
        var id = node.Properties["id"].As<string>();
        if (result.Nodes.Any(n => n.Id == id)) return;
        result.Nodes.Add(new GraphNode
        {
            Id = id,
            Label = node.Properties["title"].As<string>(),
            Group = "Movie",
            Properties = new Dictionary<string, object?> { ["year"] = node.Properties.GetValueOrDefault("year") }
        });
    }

    private static void AddPersonNode(GraphResult result, INode node)
    {
        var id = node.Properties["id"].As<string>();
        if (result.Nodes.Any(n => n.Id == id)) return;
        result.Nodes.Add(new GraphNode
        {
            Id = id,
            Label = node.Properties["name"].As<string>(),
            Group = "Person"
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_driver is not null)
        {
            await _driver.DisposeAsync();
        }
    }
}
