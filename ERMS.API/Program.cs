using ERMS.API;
using ERMS.Application;
using ERMS.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddWebApi(builder.Configuration);

builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapOpenApi();
app.MapScalarApiReference(options => {
    options.Title = "ERMS System API";
    options.Theme = ScalarTheme.Mars;
    options.ShowSidebar = true;
});

app.UseCors("AllowFrontend");

// Only redirect to HTTPS in production (prevents CORS preflight issues in dev)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRateLimiter(); 
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

app.Run();