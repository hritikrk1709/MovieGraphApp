# Reel Graph — Movie Recommendations on CognoDB

A full-stack graph application built with ASP.NET Core and a Neo4j-compatible database that helps users:

- discover movie recommendations from shared user taste
- explore actor/director connections across films
- visualize the connected graph around a movie or shortest path between actors

Demo: https://your-app-link.example

## Why a graph database?

Recommendation systems and relationship-heavy questions are naturally graph problems, not relational-table problems:

- Collaborative filtering is a multi-hop traversal: `User -> RATED -> Movie <- RATED <- OtherUser -> RATED -> Movie`.
- Shortest path between actors requires exploring connected movies, not fixed joins.
- A movie graph has rich relationships: `ACTED_IN`, `DIRECTED`, `IN_GENRE`, and `RATED`.

In a relational database, these queries require expensive self-joins or recursive CTEs. In a graph database, the traversal is direct and efficient, and the data model matches the real-world relationships.

## Data model

```text
(Person)-[:ACTED_IN {role}]->(Movie)
(Person)-[:DIRECTED]->(Movie)
(Movie)-[:IN_GENRE]->(Genre)
(User)-[:RATED {rating, timestamp}]->(Movie)
```

```text
   ┌────────┐  ACTED_IN{role}   ┌─────────┐   IN_GENRE   ┌────────┐
   │ Person │ ───────────────▶ │  Movie  │ ────────────▶ │ Genre  │
   └────────┘                   └─────────┘               └────────┘
        │  DIRECTED                  ▲
        └───────────────────────────┘│
                                      │ RATED{rating,timestamp}
                                 ┌────────┐
                                 │  User  │
                                 └────────┘
```

Core node types:

- `Movie {id, title, year, plot, posterUrl}`
- `Person {id, name}`
- `Genre {name}`
- `User {id, name}`

The app seeds a compact dataset of movies, actors, genres, viewers, and ratings so the graph queries return meaningful results immediately.

## Key queries

### 1. Collaborative filtering recommendations

```cypher
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
```

This powers the “Recommended For You” feature.

### 2. Degrees of separation

```cypher
MATCH (a:Person {id: $fromId}), (b:Person {id: $toId})
MATCH path = shortestPath((a)-[:ACTED_IN*..10]-(b))
RETURN path
```

This powers the shortest-path graph view between two actors.

### 3. Movie neighborhood graph

```cypher
MATCH (m:Movie {id: $movieId})
OPTIONAL MATCH (m)-[:IN_GENRE]->(g:Genre)
OPTIONAL MATCH (actor:Person)-[acted:ACTED_IN]->(m)
OPTIONAL MATCH (director:Person)-[:DIRECTED]->(m)
OPTIONAL MATCH (m)-[:IN_GENRE]->(:Genre)<-[:IN_GENRE]-(similar:Movie)
WHERE similar <> m
RETURN m, collect(DISTINCT g) AS genres, collect(DISTINCT actor) AS cast,
       collect(DISTINCT director) AS directors, collect(DISTINCT similar) AS similarMovies
```

This returns the full graph used for the interactive film-connection visualization.

## Project structure

```text
MovieGraphApp/
├── Program.cs                    # app startup, DI, and seed entry point
├── MovieGraphApp.csproj          # ASP.NET Core project config
├── Controllers/                  # Movies, Users, Actors, Health APIs
├── Services/Neo4jService.cs      # graph queries and driver setup
├── Data/
│   ├── SeedData.cs               # movie data and relationship seed records
│   └── Seeder.cs                 # clears, creates constraints, and loads seed graph data
├── Models/
│   └── Models.cs                 # DTO and graph models
├── frontend/                     # React + Vite frontend
├── wwwroot/                      # optional static assets / local host fallback
├── .env.example                  # env template; never commit real secrets
├── .gitignore                    # excludes local secrets and build artifacts
├── README.md                     # project documentation
└── package.json                  # local dev orchestration script
```

## Setup

### 1. Create a CognoDB Cloud instance

1. Go to https://console.cognodb.com/signup
2. Create a free instance or `c0` environment
3. Copy the generated URI and password
4. Save these in a local `.env` file

Example:

```env
NEO4J__URI=bolt+s://<instance-id>.databases.cognodb.com
NEO4J__USER=cognodb
NEO4J__PASSWORD=<generated-password>
```

### 2. Configure local environment

Copy the template and fill in your actual credentials:

```bash
cp .env.example .env
```

Then update `.env` with your real values.

### 3. Install dependencies and seed the graph

```bash
dotnet restore
dotnet run -- seed
```

This clears the current graph and loads the demo data.

### 4. Run locally

```bash
dotnet run
```

The backend runs on a local ASP.NET endpoint, and the frontend can be served through the app or via the Vite dev server.

For frontend-only local development:

```bash
cd frontend
npm install
npm run dev
```

## Deployment

This app is ready to deploy to any host that supports ASP.NET Core, including:

- Azure App Service
- Render
- Railway
- Fly.io

Set these environment variables in the hosting platform:

```env
NEO4J__URI=bolt+s://<instance-id>.databases.cognodb.com
NEO4J__USER=cognodb
NEO4J__PASSWORD=<generated-password>
```

Do not commit real credentials to GitHub.

## Public GitHub repo checklist

Before publishing publicly:

- keep `.env` out of source control
- keep only `.env.example` in the repo
- add screenshots to a `docs/screenshots/` folder
- add your hosted demo URL to the top of this README
- confirm the app runs after deployment with the configured environment variables

## Screenshots

Add screenshots like these before publishing:

- Browse Films
- Recommended For You
- Degrees of Separation
- Movie detail and graph view

Example structure:

```text
docs/
└── screenshots/
    ├── browse.png
    ├── recommendations.png
    ├── path.png
    └── movie-detail.png
```

Then reference them here:

```md
![Browse Films](docs/screenshots/browse.png)
![Recommended For You](docs/screenshots/recommendations.png)
![Degrees of Separation](docs/screenshots/path.png)
```

## License

This project is intended for learning and portfolio/demo use.

## Notes

This app demonstrates how graph databases make recommendation and relationship queries simple and expressive compared with traditional relational joins.
