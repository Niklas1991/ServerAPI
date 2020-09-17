//using AutoMapper;
//using Microsoft.Extensions.Configuration;
//using Microsoft.IdentityModel.Tokens;
//using ServerAPI.Data;
//using ServerAPI.Entities;
//using ServerAPI.Entities.Response;
//using ServerAPI.Models.Accounts;
//using ServerAPI.Models.Response;
//using System;
//using System.Collections.Generic;
//using System.IdentityModel.Tokens.Jwt;
//using System.Linq;
//using System.Security.Claims;
//using System.Security.Cryptography;
//using System.Text;
//using System.Threading.Tasks;

//namespace ServerAPI.Services
//{
//    public interface IAccountService
//    {
//        AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress);
//        AuthenticateResponse RefreshToken(string token, string ipAddress);
//        void RevokeToken(string token, string ipAddress);
//        void ValidateResetToken(ValidateResetTokenRequest model);
        
       
//        //AccountResponse GetById(int id);
        
//    }

//    public class AccountService : IAccountService
//    {
//        private readonly NorthwindContext _context;
//        private readonly IMapper _mapper;
//        private readonly IConfiguration _configuration;


//        public AccountService(
//            NorthwindContext context,
//            IMapper mapper,
//            IConfiguration configuration)
            
//        {
//            _context = context;
//            _mapper = mapper;
//            _configuration = configuration;
            
//        }

//        public Task<AuthenticateResponse> Authenticate(AuthenticateRequest model, string ipAddress)
//        {
//            var account = _context.Accounts.SingleOrDefault(x => x.Email == model.Email);

//            if (account == null || !BC.Verify(model.Password, account.PasswordHash))
//                throw new AppException("Email or password is incorrect");

//            // authentication successful so generate jwt and refresh tokens
//            var jwtToken = generateJwtToken(account);
//            var refreshToken = generateRefreshToken(ipAddress);

//            // save refresh token
//            account.RefreshTokens.Add(refreshToken);
//            _context.Update(account);
//            _context.SaveChanges();

//            var response = _mapper.Map<AuthenticateResponse>(account);
//            response.JwtToken = jwtToken;
//            response.RefreshToken = refreshToken.Token;
//            return response;
//        }

//        public AuthenticateResponse RefreshToken(string token, string ipAddress)
//        {
//            var (refreshToken, account) = getRefreshToken(token);

//            // replace old refresh token with a new one and save
//            var newRefreshToken = generateRefreshToken(ipAddress);
//            refreshToken.Revoked = DateTime.UtcNow;
//            refreshToken.RevokedByIp = ipAddress;
//            refreshToken.ReplacedByToken = newRefreshToken.Token;
//            account.RefreshTokens.Add(newRefreshToken);
//            _context.Update(account);
//            _context.SaveChanges();

//            // generate new jwt
//            var jwtToken = generateJwtToken(account);

//            var response = _mapper.Map<AuthenticateResponse>(account);
//            response.JwtToken = jwtToken;
//            response.RefreshToken = newRefreshToken.Token;
//            return response;
//        }

//        public void RevokeToken(string token, string ipAddress)
//        {
//            var (refreshToken, account) = getRefreshToken(token);

//            // revoke token and save
//            refreshToken.Revoked = DateTime.UtcNow;
            
//            _context.Update(account);
//            _context.SaveChanges();
//        }
       
//		public void ValidateResetToken(ValidateResetTokenRequest model)
//		{
//			var account = _context.Accounts.SingleOrDefault(x =>
//				x.ResetToken == model.Token &&
//				x.ResetTokenExpires > DateTime.UtcNow);

//			if (account == null)
//				throw new AppException("Invalid token");
//		}



		

//        private string generateJwtToken(Account account)
//        {
//            var tokenHandler = new JwtSecurityTokenHandler();
//            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
//            var tokenDescriptor = new SecurityTokenDescriptor
//            {
//                Subject = new ClaimsIdentity(new[] { new Claim("id", account.Id.ToString()) }),
//                Expires = DateTime.UtcNow.AddMinutes(15),
//                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
//            };
//            var token = tokenHandler.CreateToken(tokenDescriptor);
//            return tokenHandler.WriteToken(token);
//        }

//        private (RefreshToken, Account) getRefreshToken(string token)
//        {
//            var account = _context.Accounts.SingleOrDefault(u => u.RefreshTokens.Any(t => t.Token == token));
//            if (account == null) throw new AppException("Invalid token");
//            var refreshToken = account.RefreshTokens.Single(x => x.Token == token);
//            if (!refreshToken.IsActive) throw new AppException("Invalid token");
//            return (refreshToken, account);
//        }
//        private RefreshToken generateRefreshToken(string ipAddress)
//        {
//            return new RefreshToken
//            {
//                Token = randomTokenString(),
//                Expires = DateTime.UtcNow.AddDays(7),
//                Created = DateTime.UtcNow,
                
//            };
//        }

//        private string randomTokenString()
//        {
//            using var rngCryptoServiceProvider = new RNGCryptoServiceProvider();
//            var randomBytes = new byte[40];
//            rngCryptoServiceProvider.GetBytes(randomBytes);
            
//            return BitConverter.ToString(randomBytes).Replace("-", "");
//        }
        
//        ////public void ValidateResetToken(ValidateResetTokenRequest model)
//        ////{
//        ////	throw new NotImplementedException();
//        ////}


//    }
//}

