using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace U_Ride.Models
{
    public class Booking
    {
        [Key]
        public int BookingID { get; set; }

        [ForeignKey("RideID")]
        public int RideID { get; set; }

        [ForeignKey("UserID")]
        public int StudentID { get; set; }
        
        public DateTime BookingDate { get; set; }
    }
}
