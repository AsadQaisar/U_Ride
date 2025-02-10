using Microsoft.EntityFrameworkCore;

namespace U_Ride.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }

        public DbSet<Vehicle> Vehicles { get; set; }

        public DbSet<Ride> Rides { get; set; }

        public DbSet<Booking> Bookings { get; set; }

        public DbSet<Chat> Chats { get; set; }

        public DbSet<Message> Messages { get; set; }

        public DbSet<BlacklistedToken> BlacklistedTokens { get; set; }

    }
}
