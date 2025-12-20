using ERMS.Application.Interface;
using ERMS.Domain.Entities;
using ERMS.Infrastructure.Data;
using ERMS.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. DB Context
            services.AddDbContext<ERMSDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // 2. Identity
            services.AddIdentityCore<User>()
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<ERMSDbContext>();


            services.AddScoped<ITokenService, TokenService>();





            return services;
        }
    }
}
