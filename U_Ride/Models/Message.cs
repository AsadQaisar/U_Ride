using System.ComponentModel.DataAnnotations;

namespace U_Ride.Models
{
    public class Message
    {
        [Key]
        public int MessageID { get; set; } // Primary Key
        
        public int ChatID { get; set; } // Foreign Key
        
        public int SenderID { get; set; } // Foreign Key
        
        public string MessageContent { get; set; }
        
        public DateTime SentOn { get; set; } = DateTime.UtcNow;
    }
}
