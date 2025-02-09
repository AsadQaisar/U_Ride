using System.ComponentModel.DataAnnotations;

namespace U_Ride.Models
{
    public class Chat
    {
        [Key]
        public int ChatID { get; set; } // Primary Key
        
        public int ReceiverID { get; set; } // Foreign Key
        
        public int SenderID { get; set; } // Foreign Key

        public ICollection<Message>? Messages { get; set; } = new List<Message>();

        public DateTime StartedOn { get; set; } = DateTime.UtcNow;
    }
}
