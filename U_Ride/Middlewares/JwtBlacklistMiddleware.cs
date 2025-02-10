using Microsoft.EntityFrameworkCore;
using U_Ride.Models;

namespace U_Ride.Middlewares
{
    public class JwtBlacklistMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ApplicationDbContext _context;

        public JwtBlacklistMiddleware(RequestDelegate next, ApplicationDbContext context)
        {
            _next = next;
            _context = context;
        }

        public async Task Invoke(HttpContext context)
        {
            var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            if (!string.IsNullOrEmpty(token))
            {
                bool isBlacklisted = await _context.BlacklistedTokens.AnyAsync(bt => bt.Token == token);
                if (isBlacklisted)
                {
                    context.Response.StatusCode = 401; // Unauthorized
                    await context.Response.WriteAsync("Token has been revoked.");
                    return;
                }
            }

            await _next(context);
        }
    }
}
