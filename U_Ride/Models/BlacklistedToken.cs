namespace U_Ride.Models
{
    public class BlacklistedToken
    {
        public int Id { get; set; }

        public int UserID { get; set; }

        public string Token { get; set; }

        public DateTime RevokedOn { get; set; }
    }
}
