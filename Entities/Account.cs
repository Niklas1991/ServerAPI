using Microsoft.AspNetCore.Identity;
using ServerAPI.Entities.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ServerAPI.Entities
{
	public class Account : IdentityUser
	{
        public int EmployeeId { get; set; }
        public Role Role { get; set; }      
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public string VerificationToken { get; set; }
        public List<RefreshToken> RefreshTokens { get; set; }

        public bool OwnsToken(string token)
        {
            return this.RefreshTokens?.Find(x => x.Token == token) != null;
        }
    }
}
