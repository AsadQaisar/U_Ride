using Microsoft.EntityFrameworkCore;

namespace U_Ride.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }

        public DbSet<Vehicle> Vehicles { get; set; }
    }
}
