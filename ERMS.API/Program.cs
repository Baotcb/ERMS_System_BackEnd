using ERMS.API;
using ERMS.Application;
using ERMS.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Scalar.AspNetCore;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

Env.Load();
// Add services to the container.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddWebApi(builder.Configuration);

builder.Services.AddHealthChecks();

var app = builder.Build();


app.MapOpenApi();
app.MapScalarApiReference(options => {
    options.Title = "ERMS System API";
    options.Theme = ScalarTheme.DeepSpace;
    options.ShowSidebar = true;
});

app.UseCors("AllowFrontend");


if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRateLimiter(); 
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapMethods("/db-health", new[] { "GET", "HEAD" }, async (IServiceProvider sp) =>
{
    var context = sp.GetRequiredService<ERMS.Application.Interface.IERMSDbContext>();
    try
    {
        var canConnect = await context.Database.CanConnectAsync();
        if (canConnect)
        {
            await context.Database.ExecuteSqlRawAsync("SELECT 1");
            return Results.Ok(new { status = "healthy", message = "Database is active", timestamp = DateTime.UtcNow });
        }
        return Results.Json(
            new { status = "unhealthy", message = "Cannot connect to database", timestamp = DateTime.UtcNow },
            statusCode: 503
        );
    }
    catch (Exception ex)
    {
        return Results.Json(
            new { status = "unhealthy", message = ex.Message, timestamp = DateTime.UtcNow },
            statusCode: 503
        );
    }
}).AllowAnonymous();

app.MapControllers();

app.Run();