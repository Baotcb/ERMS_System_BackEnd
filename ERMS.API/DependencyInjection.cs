using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

namespace ERMS.API
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddWebApi(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers();
            services.AddOpenApi();
            services.AddEndpointsApiExplorer();
            services.AddHttpContextAccessor();
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.ForwardLimit = 1;

                foreach (var proxy in configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
                {
                    if (IPAddress.TryParse(proxy, out var parsedProxy))
                    {
                        options.KnownProxies.Add(parsedProxy);
                    }
                }

                foreach (var network in configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [])
                {
                    if (TryParseCidr(network, out var parsedNetwork))
                    {
                        options.KnownIPNetworks.Add(parsedNetwork);
                    }
                }
            });



            string clientUrl = configuration["ClientSettings:Url"];
            services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins(clientUrl)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });



            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddCookie(options =>
                {
                    options.Cookie.Name = "ERMS.External";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.None;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                })
               .AddJwtBearer(options =>
               {
                 
                   options.Events = new JwtBearerEvents
                   {
                       OnMessageReceived = context =>
                       {
                         
                           var token = context.Request.Headers.Authorization.ToString();
                           if (!string.IsNullOrEmpty(token) && token.StartsWith("Bearer "))
                           {
                               context.Token = token.Substring("Bearer ".Length).Trim();
                           }
                           else
                           {
                               
                               if (context.Request.Cookies.TryGetValue("auth_token", out var cookieToken))
                               {
                                   context.Token = cookieToken;
                               }
                           }
                           return Task.CompletedTask;
                       }
                   };

                   options.TokenValidationParameters = new TokenValidationParameters
                   {
                       ValidateIssuer = true,
                       ValidateAudience = true,
                       ValidateLifetime = true,
                       ValidateIssuerSigningKey = true,
                       ValidIssuer = configuration["JwtSettings:Issuer"],
                       ValidAudience = configuration["JwtSettings:Audience"],
                       IssuerSigningKey = new SymmetricSecurityKey(
                           Encoding.UTF8.GetBytes(configuration["JwtSettings:Key"])),
                       RoleClaimType = ClaimTypes.Role,
                       NameClaimType = ClaimTypes.Name
                   };
               });

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;


                options.AddFixedWindowLimiter("fixed", limiterOptions =>
                {
                    limiterOptions.PermitLimit = 5;
                    limiterOptions.Window = TimeSpan.FromSeconds(5);
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.QueueLimit = 0;
                });

                options.AddPolicy("admin-fixed", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        GetAdminRateLimitPartitionKey(httpContext),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromSeconds(10),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }));
            });



          
            services.Configure<IdentityOptions>(options =>
            {
               
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.User.RequireUniqueEmail = true;

               
                options.SignIn.RequireConfirmedEmail = true;        
                options.SignIn.RequireConfirmedPhoneNumber = false;
                options.SignIn.RequireConfirmedAccount = false;

              
                options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
                options.Tokens.ChangeEmailTokenProvider = TokenOptions.DefaultEmailProvider;

              
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;

              
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            });



            return services;
        }

        private static bool TryParseCidr(string cidr, out System.Net.IPNetwork network)
        {
            return System.Net.IPNetwork.TryParse(cidr, out network);
        }

        private static string GetAdminRateLimitPartitionKey(HttpContext context)
        {
            var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(adminId))
            {
                return $"admin-user:{adminId}";
            }

            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            return string.IsNullOrWhiteSpace(ipAddress) ? "admin-user:anonymous" : $"admin-ip:{ipAddress}";
        }
    }
}
