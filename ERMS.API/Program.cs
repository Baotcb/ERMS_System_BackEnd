using ERMS.API;
using ERMS.Application;
using ERMS.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


builder.Services.AddWebApi(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();





var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.MapOpenApi();
    app.MapScalarApiReference(options => {
        options.Title = "ERMS System API";
        options.Theme = ScalarTheme.Mars;
        options.ShowSidebar = true;
    });
//}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseRateLimiter(); 

app.UseAuthorization();

app.MapControllers();

app.Run();
