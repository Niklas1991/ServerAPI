using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServerAPI.Data;
using ServerAPI.Entities;
using ServerAPI.Entities.Response;
using ServerAPI.Helpers;
using ServerAPI.Models.Accounts;
using ServerAPI.Models.Response;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ServerAPI.Services
{
	public interface IAccountService
	{
		Task<AuthenticateResponse> Authenticate(AuthenticateRequest model);
		AuthenticateResponse RefreshToken(string token, string ipAddress);
		void RevokeToken(string token, string ipAddress);
		//void ValidateResetToken(ValidateResetTokenRequest model);


		//AccountResponse GetById(int id);

	}

	public class AccountService : IAccountService
	{
		private readonly NorthwindContext _context;
		private readonly IMapper _mapper;
		private readonly IConfiguration _configuration;
		private readonly AppSettings _appSettings;
		private readonly UserManager<Account> userManager;
		private readonly RoleManager<IdentityRole> roleManager;


		public AccountService(
			NorthwindContext context,
			IMapper mapper,
			IOptions<AppSettings> appSettings,
			IConfiguration configuration,
			UserManager<Account> _userManager,
			RoleManager<IdentityRole> _roleManager)

		{
			_context = context;
			_mapper = mapper;
			_configuration = configuration;
			_appSettings = appSettings.Value;
			userManager = _userManager;
			roleManager = _roleManager;
		}

		public async Task<AuthenticateResponse> Authenticate(AuthenticateRequest model)
		{
			var user = await userManager.FindByNameAsync(model.UserName);
			if (user != null && await userManager.CheckPasswordAsync(user, model.Password))
			{
				// authentication successful so generate jwt and refresh tokens
				var jwtToken = generateJwtToken(user);
				var refreshToken = generateRefreshToken();

				// save refresh token
				user.RefreshTokens.Add(refreshToken);
				_context.Update(user);
				_context.SaveChanges();

				var response = _mapper.Map<AuthenticateResponse>(user);
				response.JwtToken = jwtToken;
				response.RefreshToken = refreshToken.Token;
				return response;
			}
			return null;
		}

		public AuthenticateResponse RefreshToken(string token, string ipAddress)
		{
			var (refreshToken, account) = getRefreshToken(token);

			// replace old refresh token with a new one and save
			var newRefreshToken = generateRefreshToken();
			refreshToken.Revoked = DateTime.UtcNow;
			//refreshToken.RevokedByIp = ipAddress;
			refreshToken.ReplacedByToken = newRefreshToken.Token;
			account.RefreshTokens.Add(newRefreshToken);
			_context.Update(account);
			_context.SaveChanges();

			// generate new jwt
			var jwtToken = generateJwtToken(account);

			var response = _mapper.Map<AuthenticateResponse>(account);
			response.JwtToken = jwtToken;
			response.RefreshToken = newRefreshToken.Token;
			return response;
		}

		public void RevokeToken(string token, string ipAddress)
		{
			var (refreshToken, account) = getRefreshToken(token);

			// revoke token and save
			refreshToken.Revoked = DateTime.UtcNow;

			_context.Update(account);
			_context.SaveChanges();
		}

		//public void ValidateResetToken(ValidateResetTokenRequest model)
		//{
		//	var account = _context.Users.SingleOrDefault(x =>
		//		x.ResetToken == model.Token &&
		//		x.ResetTokenExpires > DateTime.UtcNow);

		//	if (account == null)
		//		throw new AppException("Invalid token");
		//}





		private string generateJwtToken(Account account)
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(new[] { new Claim("id", account.Id.ToString()) }),
				Expires = DateTime.UtcNow.AddMinutes(15),
				SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
			};
			var token = tokenHandler.CreateToken(tokenDescriptor);
			return tokenHandler.WriteToken(token);
		}

		private (RefreshToken, Account) getRefreshToken(string token)
		{
			var account = _context.Users.SingleOrDefault(u => u.RefreshTokens.Any(t => t.Token == token));
			if (account == null) throw new AppException("Invalid token");
			var refreshToken = account.RefreshTokens.Single(x => x.Token == token);
			if (!refreshToken.IsActive) throw new AppException("Invalid token");
			return (refreshToken, account);
		}
		private RefreshToken generateRefreshToken()
		{
			return new RefreshToken
			{
				Token = randomTokenString(),
				Expires = DateTime.UtcNow.AddDays(7),
				Created = DateTime.UtcNow,

			};
		}

		private string randomTokenString()
		{
			using var rngCryptoServiceProvider = new RNGCryptoServiceProvider();
			var randomBytes = new byte[40];
			rngCryptoServiceProvider.GetBytes(randomBytes);

			return BitConverter.ToString(randomBytes).Replace("-", "");
		}

		


	}
}

