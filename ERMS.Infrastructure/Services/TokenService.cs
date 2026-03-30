using ERMS.Application.Interface;
using Microsoft.EntityFrameworkCore;
using ERMS.Domain.Entities;
using ERMS.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace ERMS.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManager;
        private readonly IERMSDbContext _context;

        public TokenService(IConfiguration configuration, UserManager<User> userManager, IERMSDbContext context)
        {
            _configuration = configuration;
            _userManager = userManager;
            _context = context;
        }
        public async Task<string> CreateToken(User user)
        {
            var isTrainer = await _context.Employees
                .AnyAsync(e => e.UserId == user.Id && e.IsTrainer && !e.IsDeleted);

            var roles = await  _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.GivenName, user.FullName ?? string.Empty),
                new Claim("isTrainer", isTrainer.ToString().ToLower())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddMinutes(double.Parse(_configuration["JwtSettings:DurationInMinutes"])),
                SigningCredentials = creds,
                Issuer = _configuration["JwtSettings:Issuer"],
                Audience = _configuration["JwtSettings:Audience"]
            };


            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);

        }
    }
}
