using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace U_Ride.Models
{
    public class Booking
    {
        [Key]
        public int BookingID { get; set; }

        public int RideID { get; set; }

        public int UserID { get; set; }
        
        public DateTime BookingDate { get; set; }
    }
}
