using MovieGraphApp.Services;

namespace MovieGraphApp.Data;

public static class Seeder
{
    public static async Task RunAsync(Neo4jService neo4j)
    {
        Console.WriteLine("Checking CognoDB connectivity...");
        if (!await neo4j.CanConnectAsync())
        {
            Console.WriteLine("Could not connect to CognoDB. Check NEO4J__URI / NEO4J__USER / NEO4J__PASSWORD.");
            Environment.Exit(1);
        }

        Console.WriteLine("Connected. Clearing existing data...");
        await neo4j.ClearDatabaseAsync();

        Console.WriteLine("Creating constraints...");
        await neo4j.EnsureConstraintsAsync();

        Console.WriteLine($"Loading {SeedData.Movies.Length} movies...");
        await neo4j.RunWriteAsync(
            """
            UNWIND $rows AS row
            CREATE (m:Movie {id: row.id, title: row.title, year: row.year, plot: row.plot,
                              posterUrl: 'https://picsum.photos/seed/' + row.id + '/300/450'})
            WITH m, row.genres AS genres
            UNWIND genres AS genreName
            MERGE (g:Genre {name: genreName})
            MERGE (m)-[:IN_GENRE]->(g)
            """,
            new Dictionary<string, object>
            {
                ["rows"] = SeedData.Movies.Select(m => new Dictionary<string, object>
                {
                    ["id"] = m.Id,
                    ["title"] = m.Title,
                    ["year"] = m.Year,
                    ["plot"] = m.Plot,
                    ["genres"] = m.Genres,
                }).ToList()
            });

        Console.WriteLine($"Loading {SeedData.Cast.Length} cast credits...");
        await neo4j.RunWriteAsync(
            """
            UNWIND $rows AS row
            MERGE (p:Person {id: row.personId})
            ON CREATE SET p.name = row.personName
            WITH p, row
            MATCH (m:Movie {id: row.movieId})
            MERGE (p)-[:ACTED_IN {role: row.role}]->(m)
            """,
            new Dictionary<string, object>
            {
                ["rows"] = SeedData.Cast.Select(c => new Dictionary<string, object>
                {
                    ["personId"] = c.PersonId,
                    ["personName"] = c.PersonName,
                    ["movieId"] = c.MovieId,
                    ["role"] = c.Role,
                }).ToList()
            });

        Console.WriteLine($"Loading {SeedData.Directors.Length} director credits...");
        await neo4j.RunWriteAsync(
            """
            UNWIND $rows AS row
            MERGE (p:Person {id: row.personId})
            ON CREATE SET p.name = row.personName
            WITH p, row
            MATCH (m:Movie {id: row.movieId})
            MERGE (p)-[:DIRECTED]->(m)
            """,
            new Dictionary<string, object>
            {
                ["rows"] = SeedData.Directors.Select(d => new Dictionary<string, object>
                {
                    ["personId"] = d.PersonId,
                    ["personName"] = d.PersonName,
                    ["movieId"] = d.MovieId,
                }).ToList()
            });

        Console.WriteLine($"Loading {SeedData.Users.Length} users...");
        await neo4j.RunWriteAsync(
            "UNWIND $rows AS row CREATE (u:User {id: row.id, name: row.name})",
            new Dictionary<string, object>
            {
                ["rows"] = SeedData.Users.Select(u => new Dictionary<string, object>
                {
                    ["id"] = u.Id,
                    ["name"] = u.Name,
                }).ToList()
            });

        Console.WriteLine($"Loading {SeedData.Ratings.Length} ratings...");
        await neo4j.RunWriteAsync(
            """
            UNWIND $rows AS row
            MATCH (u:User {id: row.userId})
            MATCH (m:Movie {id: row.movieId})
            MERGE (u)-[r:RATED]->(m)
            SET r.rating = row.rating, r.timestamp = timestamp()
            """,
            new Dictionary<string, object>
            {
                ["rows"] = SeedData.Ratings.Select(r => new Dictionary<string, object>
                {
                    ["userId"] = r.UserId,
                    ["movieId"] = r.MovieId,
                    ["rating"] = r.Rating,
                }).ToList()
            });

        Console.WriteLine("Done! Database seeded successfully.");
    }
}
