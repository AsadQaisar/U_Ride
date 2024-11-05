using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace U_Ride.Models
{
    public class Vehicle
    {
        [Key]
        public int VehicleID { get; set; }

        [ForeignKey("UserID")]
        public int UserID { get; set; }

        public virtual User User { get; set; }

        public string Make { get; set; } 

        public string Model { get; set; }

        public string LicensePlate { get; set; }

        public int Year { get; set; } 

        public string Color { get; set; }

        public int SeatCapacity { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
