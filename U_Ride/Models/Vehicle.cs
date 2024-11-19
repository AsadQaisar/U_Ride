using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace U_Ride.Models
{
    public class Vehicle
    {
        [Key]
        public int VehicleID { get; set; }

        public int UserID { get; set; }

        public string Make { get; set; } 

        public string Model { get; set; }

        public string LicensePlate { get; set; }

        public int Year { get; set; } 

        public string Color { get; set; }

        public string VehicleType { get; set; }

        public int SeatCapacity { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime LastModifiedOn { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
