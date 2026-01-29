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

namespace ERMS.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
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
            services.AddScoped<ITokenService, TokenService>();
            services.AddTransient<IEmailService, EmailService>();





            return services;
        }
    }
}
