using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace U_Ride.Models
{
    public class User
    {
        [Key]       
        public int UserID { get; set; }
        
        public string FullName { get; set; }
        
        public string Gender { get; set; }
        
        public string? SeatNumber { get; set; }
        
        public string? Department { get; set; }
        
        public string? Email { get; set; }
        
        public string? PhoneNumber { get; set; }
        
        public string Password { get; set; }
        
        public DateTime CreatedOn { get; set; }
        
        public DateTime LastModifiedOn { get; set; }
        
        public bool? HasVehicle { get; set; }

        public double? DriverRating { get; set; }
        
        public bool IsActive { get; set; }

        public Ride? Ride { get; set; }

        public ICollection<Booking>? Bookings { get; set; } = new List<Booking>();

        public Vehicle? Vehicle { get; set; }
    }
}
