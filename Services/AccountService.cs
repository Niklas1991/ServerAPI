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
using Microsoft.AspNetCore.Http;

namespace ServerAPI.Services
{
	public interface IAccountService
	{
		Task<AuthenticateResponse> Authenticate(AuthenticateRequest model);
		Task<AuthenticateResponse> RefreshToken(string token);
		Task RevokeToken(string token);
		Task<AccountResponse> UpdateUser([FromBody] UpdateRequest model, ClaimsPrincipal user);

		//void ValidateResetToken(ValidateResetTokenRequest model);




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
				var jwtToken = await GenerateJWTToken(user);
				var refreshToken = GenerateRefreshToken();

				// save refresh token
				user.RefreshTokens.Add(refreshToken);
				var result = await userManager.UpdateAsync(user);
				if (!result.Succeeded)
				{

				}

				var response = _mapper.Map<AuthenticateResponse>(user);
				response.JwtToken = jwtToken;
				response.RefreshToken = refreshToken.Token;
				return response;
			}
			return null;
		}

		public async Task<AuthenticateResponse> RefreshToken(string token)
		{
			var (refreshToken, account) = GetRefreshToken(token);

			// replace old refresh token with a new one and save
			var newRefreshToken = GenerateRefreshToken();
			refreshToken.Revoked = DateTime.UtcNow;
			//refreshToken.RevokedByIp = ipAddress;
			refreshToken.ReplacedByToken = newRefreshToken.Token;
			account.RefreshTokens.Add(newRefreshToken);
			var result = await userManager.UpdateAsync(account);
			if (!result.Succeeded)
				throw new AppException("Refreshtoken could not be added!");

			// generate new jwt
			var jwtToken = await GenerateJWTToken(account);

			var response = _mapper.Map<AuthenticateResponse>(account);
			response.JwtToken = jwtToken;
			response.RefreshToken = newRefreshToken.Token;
			return response;
		}

		public async Task RevokeToken(string token)
		{
			var (refreshToken, account) = GetRefreshToken(token);

			// revoke token and save
			refreshToken.Revoked = DateTime.UtcNow;

			var result = await userManager.UpdateAsync(account);
			if (!result.Succeeded)
				throw new AppException("Tokenrevoke failed!");
			
			
		}

		public async Task<AccountResponse> UpdateUser([FromBody] UpdateRequest model, ClaimsPrincipal user)
		{
			var userToUpdate = await userManager.FindByNameAsync(model.UserName);
			
		
			if (userToUpdate == null)
				throw new AppException("Error, no user found!");

			if (user.Identity.Name != userToUpdate.UserName && user.Claims.Where(s => s.Type == model.Role).Any(s => s.Value == "Admin") == false)
				throw new AppException("Unauthorized to update this user!");
			
			var mappedUser = _mapper.Map(model, userToUpdate);

			var result = await userManager.UpdateAsync(mappedUser);

			if (!result.Succeeded)
				throw new AppException("User update failed! Please check user details and try again.");
			mappedUser.Updated = DateTime.Now;
			var mappedResult = _mapper.Map<AccountResponse>(mappedUser);
			return mappedResult;

		}

		//public void ValidateResetToken(ValidateResetTokenRequest model)
		//{
		//	var account = _context.Users.SingleOrDefault(x =>
		//		x.ResetToken == model.Token &&
		//		x.ResetTokenExpires > DateTime.UtcNow);

		//	if (account == null)
		//		throw new AppException("Invalid token");
		//}





		private async Task<string> GenerateJWTToken(Account account)
		{
			var userRoles = await userManager.GetRolesAsync(account);
			var authClaims = new List<Claim>
				{
					new Claim(ClaimTypes.Name, account.UserName),
					new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
				};

			foreach (var userRole in userRoles)
			{
				authClaims.Add(new Claim(ClaimTypes.Role, userRole));
			}

			
			var key = Encoding.ASCII.GetBytes(_configuration["JWT:Secret"]);
			var tokenDescriptor = new JwtSecurityToken(
				issuer: _configuration["JWT:ValidIssuer"],
				audience: _configuration["JWT:ValidAudience"],
				expires: DateTime.UtcNow.AddMinutes(10),
				claims: authClaims,
				signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
				
			);
			var token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
			return token;
		}

		private (RefreshToken, Account) GetRefreshToken(string token)
		{
			var account = _context.Users.SingleOrDefault(u => u.RefreshTokens.Any(t => t.Token == token));
			if (account == null) throw new AppException("Invalid token");
			var refreshToken = account.RefreshTokens.Single(x => x.Token == token);
			if (!refreshToken.IsActive) throw new AppException("Invalid token");
			return (refreshToken, account);
		}
		private RefreshToken GenerateRefreshToken()
		{
			return new RefreshToken
			{
				Token = RandomTokenString(),
				Expires = DateTime.UtcNow.AddDays(7),
				Created = DateTime.UtcNow,

			};
		}

		private string RandomTokenString()
		{
			using var rngCryptoServiceProvider = new RNGCryptoServiceProvider();
			var randomBytes = new byte[40];
			rngCryptoServiceProvider.GetBytes(randomBytes);

			return BitConverter.ToString(randomBytes).Replace("-", "");
		}

		


	}
}

