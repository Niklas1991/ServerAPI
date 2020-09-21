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

        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] AuthenticateRequest model)
        {
            var response = await accountService.Authenticate(model);
            //setTokenCookie(response.RefreshToken);
            return Ok(response);
        }
        [Authorize]
        [HttpPatch("update-employee")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateRequest model)
        {
            
            var user = Request.HttpContext.User;
            var response = await accountService.UpdateUser(model, user);
            //setTokenCookie(response.RefreshToken);
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
                return StatusCode(StatusCodes.Status400BadRequest, new StatusResponse { Status = "Error", Message = "User already exists with that username!" });

            //Finds employee with the specified EmployeeID
            string query = @"Select * FROM Employees WHERE EmployeeID = @EmployeeID ";
            using (SqlConnection connection = new SqlConnection(configuration.GetConnectionString("DataContext")))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@EmployeeID", model.EmployeeId);
                var sqlResult = await command.ExecuteScalarAsync();

                if (sqlResult == null)
                    throw new Exception("Employee does not exist.");
            };
                      
            var user = mapper.Map<Account>(model);
           
            if (employeeExists != null)
            {
                user.EmployeeId = model.EmployeeId;
                return StatusCode(StatusCodes.Status202Accepted, new StatusResponse { Status = "Username & Employee linked!", Message = "User linked to Employee succeeded!" });
            }
            if (!await roleManager.RoleExistsAsync(Role.Employee.ToString()))
                await roleManager.CreateAsync(new IdentityRole(Role.Employee.ToString()));
            if (await roleManager.RoleExistsAsync(Role.Employee.ToString()))
                await userManager.AddToRoleAsync(user, Role.Employee.ToString());
            var result = await userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse { Status = "Error", Message = "User creation failed! Please check user details and try again." });

            return Ok(new StatusResponse { Status = "Success", Message = "User created successfully!" });
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
                command.Parameters.AddWithValue("@EmployeeID", model.EmployeeId);
            };

            var userExists = await userManager.FindByNameAsync(model.UserName);
            var employeeExists = await userManager.FindByIdAsync(model.EmployeeId.ToString());
            if (userExists != null)
                return StatusCode(StatusCodes.Status400BadRequest, new StatusResponse { Status = "Error", Message = "User already exists!" });

            
            var user = mapper.Map<Account>(model);

            if (employeeExists != null)
            {
                user.EmployeeId = model.EmployeeId;
                return StatusCode(StatusCodes.Status202Accepted, new StatusResponse { Status = "Username & Employee linked!", Message = "User linked to Employee succeeded!" });
            }

            if (!await roleManager.RoleExistsAsync(Role.Admin.ToString()))
                await roleManager.CreateAsync(new IdentityRole(Role.Admin.ToString()));
            if (await roleManager.RoleExistsAsync(Role.Employee.ToString()))
                await userManager.AddToRoleAsync(user, Role.Admin.ToString());


            var result = await userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
			{
                if (!result.Succeeded)
                    return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse { Status = "Error", Message = "User creation failed! Please check user details and try again." });

            }
            return Ok(new StatusResponse { Status = "Success", Message = "Admin User created successfully!" });
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
                command.Parameters.AddWithValue("@EmployeeID", model.EmployeeId);
            };

            var userExists = await userManager.FindByNameAsync(model.UserName);
            var employeeExists = await userManager.FindByIdAsync(model.EmployeeId.ToString());
            if (userExists != null)
                return StatusCode(StatusCodes.Status400BadRequest, new StatusResponse { Status = "Error", Message = "User already exists with that username!" });

            var user = mapper.Map<Account>(model);

            if (employeeExists != null)
            {
                user.EmployeeId = model.EmployeeId;
                return StatusCode(StatusCodes.Status202Accepted, new StatusResponse { Status = "Username & Employee linked!", Message = "User linked to Employee succeeded!" });
            }
            if (!await roleManager.RoleExistsAsync(Role.Employee.ToString()))
                await roleManager.CreateAsync(new IdentityRole(Role.Employee.ToString()));
            if (await roleManager.RoleExistsAsync(Role.Employee.ToString()))
                await userManager.AddToRoleAsync(user, Role.Employee.ToString());
            if (!await roleManager.RoleExistsAsync(Role.VD.ToString()))
                await roleManager.CreateAsync(new IdentityRole(Role.VD.ToString()));
            if (await roleManager.RoleExistsAsync(Role.VD.ToString()))
                await userManager.AddToRoleAsync(user, Role.VD.ToString());
            var result = await userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse { Status = "Error", Message = "User creation failed! Please check user details and try again." });

            return Ok(new StatusResponse { Status = "Success", Message = "User created successfully!" });
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
                command.Parameters.AddWithValue("@EmployeeID", model.EmployeeId);
            };

            var userExists = await userManager.FindByNameAsync(model.UserName);
            var employeeExists = await userManager.FindByIdAsync(model.EmployeeId.ToString());
            if (userExists != null)
                return StatusCode(StatusCodes.Status400BadRequest, new StatusResponse { Status = "Error", Message = "User already exists with that username!" });

            var user = mapper.Map<Account>(model);

            if (employeeExists != null)
            {
                user.EmployeeId = model.EmployeeId;
                return StatusCode(StatusCodes.Status202Accepted, new StatusResponse { Status = "Username & Employee linked!", Message = "User linked to Employee succeeded!" });
            }
            if (!await roleManager.RoleExistsAsync(Role.Employee.ToString()))
                await roleManager.CreateAsync(new IdentityRole(Role.Employee.ToString()));
            if (await roleManager.RoleExistsAsync(Role.Employee.ToString()))
                await userManager.AddToRoleAsync(user, Role.Employee.ToString());
            var result = await userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse { Status = "Error", Message = "User creation failed! Please check user details and try again." });

            return Ok(new StatusResponse { Status = "Success", Message = "User created successfully!" });
        }

		//[Authorize(Roles = "VD")]
		[HttpGet]
		[Route("get-all-users")]
		public async Task<IActionResult> GetAllUsers()
		{
			var users = await userManager.Users.ToListAsync();
			if (users == null)
				return StatusCode(StatusCodes.Status404NotFound, new StatusResponse { Status = "Error", Message = "No users found!" });

			var mappedResult = mapper.Map<IEnumerable<UserResponse>>(users);
			return Ok(mappedResult);
		}


		[Authorize(Roles = "Admin")]
		[HttpDelete]
		[Route("delete")]
		public async Task<IActionResult> Delete(string userName)
		{
			var userToDelete = await userManager.FindByNameAsync(userName);
		
			if (userToDelete == null)
				return StatusCode(StatusCodes.Status400BadRequest, new StatusResponse { Status = "Error", Message = "User not found" });
            			
			var result = await userManager.DeleteAsync(userToDelete);
			if (!result.Succeeded)
				return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse { Status = "Error", Message = "User deletion failed! Please check user details and try again." });

			return Ok(new StatusResponse { Status = "Success", Message = "User deleted successfully!" });
		}
	}
}
