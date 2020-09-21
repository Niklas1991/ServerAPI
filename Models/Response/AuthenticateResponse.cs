using System;
using System.Text.Json.Serialization;

namespace ServerAPI.Models.Response
{
    public class AuthenticateResponse
    {
        
        public string Email { get; set; }
        public string Role { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public string JwtToken { get; set; }

        public string RefreshToken { get; set; }
    }
}