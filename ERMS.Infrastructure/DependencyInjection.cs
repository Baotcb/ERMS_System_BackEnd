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
using Hangfire;

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
            services.Configure<GroqSettings>(configuration.GetSection("Groq"));
            services.Configure<PayOSSettings>(configuration.GetSection("PayOS"));

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
            services.AddScoped<IPayOSService, PayOSService>();
            services.AddScoped<ISubscriptionLimitChecker, SubscriptionLimitChecker>();
            // services.AddSingleton<IAIServiceConfiguration, AIServiceConfiguration>(); // Gemini — kept for reference
            services.AddSingleton<IAIServiceConfiguration, GroqAIServiceConfiguration>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<ISystemIntegrationStatusService, SystemIntegrationStatusService>();
            services.AddTransient<IEmailService, EmailService>();
            services.AddTransient<IRejectionEmailService, RejectionEmailService>();
            services.AddScoped<IExcelParserService, ExcelParserService>();
            services.AddScoped<IGoogleAuthService, GoogleAuthService>();
            // services.AddHttpClient<IGeminiProbeService, GeminiProbeService>(client => { client.Timeout = TimeSpan.FromSeconds(10); }); // Gemini — kept for reference
            services.AddHttpClient<IAIProbeService, GroqProbeService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            });

            // CV Processing Services
            services.AddScoped<ICloudinaryService, CloudinaryService>();
            services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
            // services.AddHttpClient<IGeminiAIService, GeminiAIService>(); // Gemini — kept for reference
            services.AddHttpClient<IAIService, GroqAIService>();



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

            // Hangfire setup
            services.AddHangfire(configurationHangfire => configurationHangfire
                .SetDataCompatibilityLevel(Hangfire.CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection")));

            services.AddHangfireServer();
            services.AddScoped<IDatabaseSyncJob, DatabaseSyncJob>();

            return services;
        }
    }
}
