using System.ComponentModel.DataAnnotations;

namespace U_Ride.Models
{
    public class Chat
    {
        [Key]
        public int ChatID { get; set; } // Primary Key
        
        public int StudentID { get; set; } // Foreign Key
        
        public int DriverID { get; set; } // Foreign Key

        public ICollection<Message>? Messages { get; set; } = new List<Message>();

        public DateTime StartedOn { get; set; } = DateTime.UtcNow;
    }
}
