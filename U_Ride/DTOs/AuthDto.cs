namespace U_Ride.DTOs
{
    public class AuthDto
    {
        public class RegisterDto
        {
            public int? UserID { get; set; }
            
            public string FullName { get; set; }
            
            public string Gender { get; set; }
            
            public string SeatNumber { get; set; }
            
            public string Department { get; set; }
            
            //public string Email { get; set; }
            public string PhoneNumber { get; set; }
            
            public string Password { get; set; }
        }

        public class LoginDto
        {
            public string Phone_SeatNumber { get; set; }
            public string Password { get; set; }
        }

        //================================================================================//

        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);

        public class UserInfo
        {
            public int UserID { get; set; }

            public string FullName { get; set; }

            public string SeatNumber { get; set; }

            public string Gender { get; set; }

            public string PhoneNumber { get; set; }

            public VehicleInfo? Vehicle { get; set; }

            public Authorization Authorization { get; set; }
        }

        public class VehicleInfo
        {
            public string VehicleType { get; set; }

            public string Make_Model { get; set; }

            public string LicensePlate { get; set; }
        }

        public class Authorization
        {
            public string token { get; set; }
        }

        //================================================================================//

        public class IsDriverDto
        {
            public string Make { get; set; }

            public string Model { get; set; }

            public string LicensePlate { get; set; }

            public int Year { get; set; }

            public string Color { get; set; }

            public string VehicleType { get; set; }

            public int SeatCapacity { get; set; }
        }
    }
}
