﻿using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using U_Ride.Models;

namespace U_Ride.Services
{
    public class JwtTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context; // Inject database context

        public JwtTokenService(IConfiguration configuration, ApplicationDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public string GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWTSettings:TokenKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.SeatNumber ?? user.PhoneNumber),
                new Claim("UserID", user.UserID.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["JWTSettings:Issuer"],
                audience: _configuration["JWTSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(double.Parse(_configuration["JWTSettings:DurationInMinutes"])),
                signingCredentials: creds
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<bool> IsTokenBlacklisted(string token)
        {
            return await _context.BlacklistedTokens.AsNoTracking().AnyAsync(t => t.Token == token);
        }
    }
}
