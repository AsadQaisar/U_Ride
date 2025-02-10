using U_Ride.Models;

namespace U_Ride.Services
{
    public class BlacklistedTokenCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public BlacklistedTokenCleanupService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var expirationTime = DateTime.UtcNow.AddDays(-7); // Remove tokens older than 7 days
                    dbContext.BlacklistedTokens.RemoveRange(dbContext.BlacklistedTokens.Where(t => t.RevokedOn < expirationTime));
                    await dbContext.SaveChangesAsync();
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken); // Run daily
            }
        }
    }

}
