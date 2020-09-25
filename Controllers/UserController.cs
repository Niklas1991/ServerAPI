using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using ServerAPI.Models;
using ServerAPI.Data;
using ServerAPI.Entities;
using ServerAPI.Models.Response;
using Microsoft.Data.SqlClient;
using ServerAPI.Services;

namespace ServerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserManager<Account> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IConfiguration configuration;
        private readonly IMapper mapper;
        private readonly NorthwindContext context;
        private readonly IAccountService accountService;

        public UserController(UserManager<Account> _userManager, RoleManager<IdentityRole> _roleManager, IConfiguration _configuration, IMapper _mapper, NorthwindContext _context, IAccountService _accounService)
        {
            userManager = _userManager;
            roleManager = _roleManager;
            configuration = _configuration;
            mapper = _mapper;
            context = _context;
            accountService = _accounService;
        }
        //LOGIN endpoint
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] AuthenticateRequest model)
        {
            var response = await accountService.Authenticate(model);
            
            return Ok(response);
        }
        [HttpPost("refresh-token")]
        public ActionResult<AuthenticateResponse> RefreshToken(RefreshTokenRequest refreshToken)
        {
            
            var response = accountService.RefreshToken(refreshToken.RefreshToken);
            
            return Ok(response);
        }
        [Authorize]
        [HttpPatch("update-employee")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateRequest model)
        {
            
            var user = Request.HttpContext.User;
            var response = await accountService.UpdateUser(model, user);
            if (response.Value == null)
			{
                return response.Result;
			}
            return Ok(response);
        }
        
        [HttpPost]
        [Route("register-employee")]
        public async Task<IActionResult> RegisterEmployee([FromBody] RegisterRequest model)
        {
            //Checks if a user exists with Username specified
            var userExists = await userManager.FindByNameAsync(model.UserName);
            var employeeExists = userManager.Users.Where(x => x.EmployeeId == model.EmployeeId);
            if (userExists != null)
            {
                return BadRequest("! Username already taken!");
            }
            var employeeInUse = context.Users.Where(x => x.EmployeeId == model.EmployeeId).FirstOrDefault();
            if (employeeInUse != null)
            {
                return BadRequest("! Employee already linked!");
            }
            var emailInUse = context.Users.Where(x => x.Email == model.Email).FirstOrDefault();
            if (emailInUse != null)
            {                
                return BadRequest("! Email already in use!");
            }
            //Finds employee with the specified EmployeeID
            string query = @"Select * FROM Employees WHERE EmployeeID = @EmployeeID ";
            using (SqlConnection connection = new SqlConnection(configuration.GetConnectionString("DataContext")))
            {
                SqlCommand command = new SqlCommand(query, connection);
                await connection.OpenAsync();
                command.Parameters.AddWithValue("@EmployeeID", model.EmployeeId);
                var sqlResult = await command.ExecuteNonQueryAsync();

                if (sqlResult != -1)
                    throw new Exception("Employee does not exist.");
            };                      
            
            var isFirstAccount = context.Users.Count();
            if (isFirstAccount == 0)
            {
                if (!await roleManager.RoleExistsAsync(Role.Admin.ToString()))
                {
                    await roleManager.CreateAsync(new IdentityRole(Role.Admin.ToString()));
                }                
            }

            if (!await roleManager.RoleExistsAsync(Role.Employee.ToString()))
            {
                await roleManager.CreateAsync(new IdentityRole(Role.Employee.ToString()));                
            }
            var user = mapper.Map<Account>(model);
            await userManager.AddToRoleAsync(user, Role.Employee.ToString());
            await userManager.AddToRoleAsync(user, Role.Admin.ToString());
            var result = await userManager.CreateAsync(user, model.Password);
            
            if (!result.Succeeded)
			{
                return BadRequest("Internal server error");
			}
            var accountResponse = mapper.Map<AccountResponse>(model);
            return Ok(accountResponse);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [Route("register-admin")]
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterRequest model)
        {
            string query = @"Select * FROM Employees WHERE EmployeeID = @EmployeeID ";
            using (SqlConnection connection = new SqlConnection(configuration.GetConnectionString("DataContext")))
            {
                SqlCommand command = new SqlCommand(query, connection);
                await connection.OpenAsync();
                command.Parameters.AddWithValue("@EmployeeID", model.EmployeeId);
                var sqlResult = await command.ExecuteNonQueryAsync();

                if (sqlResult != -1)
                    return BadRequest("Employee does not exist!");
            };
            var userExists = await userManager.FindByNameAsync(model.UserName);
            var employeeExists = await userManager.FindByIdAsync(model.EmployeeId.ToString());
            if (userExists != null)
			{
                return BadRequest("User already exists!");
            }
            var employeeInUse = context.Users.Where(x => x.EmployeeId == model.EmployeeId).FirstOrDefault();
            if (employeeInUse != null)
			{
                return BadRequest("Employee already linked!");
			}
            var emailInUse = context.Users.Where(x => x.Email == model.Email).FirstOrDefault();
            if (emailInUse != null)
            {
                return BadRequest("Email already in use!");
            }            	

			if (!await roleManager.RoleExistsAsync(Role.Admin.ToString()))
			{
                await roleManager.CreateAsync(new IdentityRole(Role.Admin.ToString()));                
            }
            
            if (!await roleManager.RoleExistsAsync(Role.Employee.ToString()))
			{
                await roleManager.CreateAsync(new IdentityRole(Role.Employee.ToString()));                
            }
           
            var user = mapper.Map<Account>(model);
            await userManager.AddToRoleAsync(user, Role.Employee.ToString());
            await userManager.AddToRoleAsync(user, Role.Admin.ToString());
            var result = await userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
			{
                return BadRequest("User creation failed! Please check user details and try again.");
            }
            var accountResponse = mapper.Map<AccountResponse>(model);
            return Ok(accountResponse);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [Route("register-vd")]
        public async Task<IActionResult> RegisterVD([FromBody] RegisterRequest model)
        {
            string query = @"Select * FROM Employees WHERE EmployeeID = @EmployeeID ";
            using (SqlConnection connection = new SqlConnection(configuration.GetConnectionString("DataContext")))
            {
                SqlCommand command = new SqlCommand(query, connection);
                await connection.OpenAsync();
                command.Parameters.AddWithValue("@EmployeeID", model.EmployeeId);
                var sqlResult = await command.ExecuteNonQueryAsync();

                if (sqlResult != -1)
                    return BadRequest("Employee does not exist!");
            };

            var userExists = await userManager.FindByNameAsync(model.UserName);
            var employeeExists = await userManager.FindByIdAsync(model.EmployeeId.ToString());
            if (userExists != null)
            {
                return BadRequest("User already exists!");
            }
            var employeeInUse = context.Users.Where(x => x.EmployeeId == model.EmployeeId).FirstOrDefault();
            if (employeeInUse != null)
            {
                return BadRequest("Employee already linked!");
            }
            var emailInUse = context.Users.Where(x => x.Email == model.Email).FirstOrDefault();
            if (emailInUse != null)
            {
                return BadRequest("Email already in use!");
            }
            

            if (!await roleManager.RoleExistsAsync(Role.VD.ToString()))
            {
                await roleManager.CreateAsync(new IdentityRole(Role.VD.ToString()));                
            }
            

            if (!await roleManager.RoleExistsAsync(Role.Employee.ToString()))
            {
                await roleManager.CreateAsync(new IdentityRole(Role.Employee.ToString()));                
            }
            var user = mapper.Map<Account>(model);
            await userManager.AddToRoleAsync(user, Role.VD.ToString());
            await userManager.AddToRoleAsync(user, Role.Employee.ToString());
            var result = await userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                return BadRequest("User creation failed! Please check user details and try again.");
            }
            var accountResponse = mapper.Map<AccountResponse>(model);
            return Ok(accountResponse);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [Route("register-countrymanager")]
        public async Task<IActionResult> RegisterCountryManager([FromBody] RegisterRequest model)
        {
            string query = @"Select * FROM Employees WHERE EmployeeID = @EmployeeID ";
            using (SqlConnection connection = new SqlConnection(configuration.GetConnectionString("DataContext")))
            {
                SqlCommand command = new SqlCommand(query, connection);
                await connection.OpenAsync();
                command.Parameters.AddWithValue("@EmployeeID", model.EmployeeId);
                var sqlResult = await command.ExecuteNonQueryAsync();

                if (sqlResult != -1)
                    return BadRequest("Employee does not exist!");
            };

            var userExists = await userManager.FindByNameAsync(model.UserName);
            var employeeExists = await userManager.FindByIdAsync(model.EmployeeId.ToString());
            if (userExists != null)
            {
                return BadRequest("User already exists!");
            }
            var employeeInUse = context.Users.Where(x => x.EmployeeId == model.EmployeeId).FirstOrDefault();
            if (employeeInUse != null)
            {
                return BadRequest("Employee already linked!");
            }
            var emailInUse = context.Users.Where(x => x.Email == model.Email).FirstOrDefault();
            if (emailInUse != null)
            {
                return BadRequest("Email already in use!");
            }
            

            if (!await roleManager.RoleExistsAsync(Role.CountryManager.ToString()))
            {
                await roleManager.CreateAsync(new IdentityRole(Role.CountryManager.ToString()));                
            }
            
            if (!await roleManager.RoleExistsAsync(Role.Employee.ToString()))
            {
                await roleManager.CreateAsync(new IdentityRole(Role.Employee.ToString()));                
            }
            var user = mapper.Map<Account>(model);
            await userManager.AddToRoleAsync(user, Role.CountryManager.ToString());
            await userManager.AddToRoleAsync(user, Role.Employee.ToString());
            var result = await userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                return BadRequest("User creation failed! Please check user details and try again.");
            }
            var accountResponse = mapper.Map<AccountResponse>(model);
            return Ok(accountResponse);
        }

		[Authorize(Roles = "VD,Admin")]
		[HttpGet]
		[Route("get-all-users")]
		public async Task<IEnumerable<AccountResponse>> GetAllUsers()
		{
			var users = await userManager.Users.ToListAsync();
			if (users == null)
			{
                return null;
			}
			var mappedResult = mapper.Map<IEnumerable<AccountResponse>>(users);
			return mappedResult;
		}


		[Authorize(Roles = "Admin")]
		[HttpDelete]
		[Route("delete")]
		public async Task<IActionResult> Delete(string userName)
		{
			var userToDelete = await userManager.FindByNameAsync(userName);

            if (userToDelete == null)
                return BadRequest("User does not exists, check your spelling.");
            			
			var result = await userManager.DeleteAsync(userToDelete);
			if (!result.Succeeded)
			{
                return BadRequest("User deletion failed!");
            }
				
			return Ok(result);
		}
	}
}
