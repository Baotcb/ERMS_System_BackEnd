using ERMS.API;
using ERMS.Application;
using ERMS.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Scalar.AspNetCore;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

Env.Load();
// Add services to the container.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddWebApi(builder.Configuration);

builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        var statusCode = exception switch
        {
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            ArgumentException => StatusCodes.Status400BadRequest,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
            DbUpdateException => StatusCodes.Status409Conflict,
            SqlException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        var detailMessage = exception?.InnerException?.Message ?? exception?.Message ?? "An unexpected error occurred.";

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            message = detailMessage,
            statusCode,
            traceId = context.TraceIdentifier
        });
    });
});


var forwardedHeaderOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeaderOptions.KnownNetworks.Clear();
forwardedHeaderOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeaderOptions);


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
