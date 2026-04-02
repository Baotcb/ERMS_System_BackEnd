using ERMS.Application.Interface;
using ERMS.Infrastructure.Data;
using ERMS.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using ERMS.Domain.Entities.Identity;

using ERMS.Infrastructure.Configuration;

namespace ERMS.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind settings using Options Pattern
            services.Configure<CloudinarySettings>(configuration.GetSection("Cloudinary"));
            services.Configure<GeminiSettings>(configuration.GetSection("Gemini"));

            // 1. DB Context
            services.AddDbContext<ERMSDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<IERMSDbContext>(provider => provider.GetRequiredService<ERMSDbContext>());


            // 2. Identity
            services.AddIdentity<User, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ERMSDbContext>()
            .AddDefaultTokenProviders();





            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddSingleton<IAIServiceConfiguration, AIServiceConfiguration>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<ISystemIntegrationStatusService, SystemIntegrationStatusService>();
            services.AddTransient<IEmailService, EmailService>();
            services.AddScoped<IExcelParserService, ExcelParserService>();
            services.AddScoped<IGoogleAuthService, GoogleAuthService>();

            // CV Processing Services
            services.AddScoped<ICloudinaryService, CloudinaryService>();
            services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
            services.AddHttpClient<IGeminiAIService, GeminiAIService>();



            services.AddScoped<ICalendarService, CalendarService>();
            services.AddHttpClient<IZoomService, ZoomService>();


            // Background CV Scoring
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddHostedService<CvScoringBackgroundService>();

            // IP geolocation proxy for public location detection.
            services.AddMemoryCache();
            services.AddHttpClient<IGeolocationService, GeolocationService>(client =>
            {
                client.BaseAddress = new Uri("http://ip-api.com/");
                client.Timeout = TimeSpan.FromSeconds(5);
            });

            return services;
        }
    }
}
