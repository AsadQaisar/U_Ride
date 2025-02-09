namespace U_Ride.DTOs
{
    public class StartChatDto
    {
        // public int? StudentId { get; set; }
        
        public int DriverId { get; set; }
    }

    public class SendMessageDto
    {
        // public int ChatId { get; set; }
        
        public int ReceiverID { get; set; }
        
        public string Message { get; set; }
    }

    public class GetMessagesDto
    {
        public List<int>? ChatIDs { get; set; } = new List<int>();
    }

    public class MessaageInfoDto
    {
        public int MessageID { get; set; }

        public string? MessageContent { get; set; }

        public string? SentOn { get; set; }

    }

    public class ChatInfoDto
    {
        public int ChatID { get; set; }

        public AuthDto.UserInfo? UserInfo { get; set; }

        public MessaageInfoDto? MessaageInfo { get; set; }
    }

}
