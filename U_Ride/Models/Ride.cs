using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace U_Ride.Models
{
    public class Ride
    {
        [Key]
        public int RideID { get; set; }

        public int UserID { get; set; }
        
        public string StartPoint { get; set; }
        
        public string EndPoint { get; set; }

        public string? EncodedPolyline { get; set; }

        public bool IsDriver { get; set; } = true;

        public double? Price { get; set; }
        
        public int? AvailableSeats { get; set; }

        public bool IsAvailable { get; set; } = true;

        public DateTime CreatedOn { get; set; }

        public DateTime LastModifiedOn { get; set; }

    }
}
