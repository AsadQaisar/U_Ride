namespace U_Ride.DTOs
{
    public class StartChatDto
    {
        // public int? StudentId { get; set; }
        
        public int DriverId { get; set; }
    }

    public class SendMessageDto
    {
        public int ChatId { get; set; }
        
        // public int? SenderId { get; set; }
        
        public string Message { get; set; }
    }

    public class JoinChatDto
    {
        public string ConnectionId { get; set; }
       
        public int ChatId { get; set; }
    }

}
