using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace U_Ride.Models
{
    public class Ride
    {
        [Key]
        public int RideID { get; set; }

        [ForeignKey("UserID")]
        public int DriverID { get; set; }
        
        public string StartPoint { get; set; }
        
        public string EndPoint { get; set; }
        
        public double? Price { get; set; }
        
        public int AvailableSeats { get; set; }

        public bool IsAvailable { get; set; } = true; // e.g., "Available", "Full"

        public DateTime CreatedOn { get; set; }

        public DateTime LastModifiedOn { get; set; }

        public virtual ICollection<Vehicle> Vehicles { get; set; }
    }
}
