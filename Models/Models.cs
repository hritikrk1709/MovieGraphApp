namespace MovieGraphApp.Models;

public class Movie
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public int Year { get; set; }
    public string Plot { get; set; } = "";
    public string PosterUrl { get; set; } = "";
    public List<string> Genres { get; set; } = new();
    public List<PersonSummary> Cast { get; set; } = new();
    public List<PersonSummary> Directors { get; set; } = new();
}

public class PersonSummary
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Role { get; set; }
}

public class MovieSummary
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public int Year { get; set; }
    public string PosterUrl { get; set; } = "";
    public List<string> Genres { get; set; } = new();
}

public class UserSummary
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}

public class Recommendation
{
    public MovieSummary Movie { get; set; } = new();
    public int Score { get; set; }
    public List<string> BecauseUsersAlsoLiked { get; set; } = new();
}

public class GraphNode
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string Group { get; set; } = ""; // Movie | Person | Genre | User
    public Dictionary<string, object?> Properties { get; set; } = new();
}

public class GraphEdge
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public string Type { get; set; } = "";
    public Dictionary<string, object?> Properties { get; set; } = new();
}

public class GraphResult
{
    public List<GraphNode> Nodes { get; set; } = new();
    public List<GraphEdge> Edges { get; set; } = new();
}

public class ActorPathResult
{
    public bool Found { get; set; }
    public int Hops { get; set; }
    public GraphResult Graph { get; set; } = new();
}
