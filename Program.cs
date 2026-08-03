using Microsoft.Extensions.FileProviders;
using MovieGraphApp.Data;
using MovieGraphApp.Services;

var builder = WebApplication.CreateBuilder(args);

var envFile = Path.Combine(builder.Environment.ContentRootPath, ".env");
if (File.Exists(envFile))
{
    foreach (var line in File.ReadAllLines(envFile))
    {
        var trimmed = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
        {
            continue;
        }

        var equalsIndex = trimmed.IndexOf('=');
        if (equalsIndex <= 0)
        {
            continue;
        }

        var key = trimmed[..equalsIndex].Trim();
        var value = trimmed[(equalsIndex + 1)..].Trim();

        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<Neo4jService>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

if (args.Contains("seed"))
{
    using var scope = app.Services.CreateScope();
    var neo4j = scope.ServiceProvider.GetRequiredService<Neo4jService>();
    await Seeder.RunAsync(neo4j);
    return;
}

app.UseCors();
app.MapControllers();

var frontendDist = Path.Combine(builder.Environment.ContentRootPath, "frontend", "dist");
if (Directory.Exists(frontendDist))
{
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = new PhysicalFileProvider(frontendDist),
        DefaultFileNames = new[] { "index.html" },
    });

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(frontendDist),
        RequestPath = "",
    });

    app.MapFallbackToFile("index.html", new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(frontendDist),
    });
}

app.Run();
