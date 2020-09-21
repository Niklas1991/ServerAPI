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
        private readonly string typeSchema = @"http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
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
        public ActionResult<AuthenticateResponse> Authenticate(AuthenticateRequest model)
        {
            var response = accountService.Authenticate(model);
            //setTokenCookie(response.RefreshToken);
            return Ok(response);
        }
        [HttpPost]
        [Route("register-employee")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
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

        //[Authorize(Roles = Policies.Admin)]
        //[HttpGet]
        //[Route("get-all-users")]
        //public async Task<IActionResult> GetAll()
        //{
        //    var users = await userManager.Users.ToListAsync();
        //    if (users == null)
        //        return StatusCode(StatusCodes.Status404NotFound, new StatusResponse { Status = "Error", Message = "No users found!" });

        //    var mappedResult = mapper.Map<IEnumerable<UserResponse>>(users);
        //    return Ok(mappedResult);
        //}

        //[Authorize]
        //[HttpPatch]
        //[Route("update")]
        //public async Task<IActionResult> Update(string username, [FromBody] UpdateUser model)
        //{
        //    var userToUpdate = await userManager.FindByNameAsync(username);
        //    var user = Request.HttpContext.User;

        //    if (userToUpdate == null)
        //        return StatusCode(StatusCodes.Status400BadRequest, new StatusResponse { Status = "Error", Message = "" });

        //    if (user.Identity.Name != userToUpdate.UserName && user.Claims.Where(s => s.Type == typeSchema).Any(s => s.Value == "Admin") == false)
        //        return StatusCode(StatusCodes.Status401Unauthorized, new StatusResponse { Status = "Error", Message = "Unathorized to update this user!" });

        //    var mappedResult = mapper.Map<UpdateRequest, Account>(model, userToUpdate);

        //    var result = await userManager.UpdateAsync(mappedResult);

        //    if (!result.Succeeded)
        //        return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse { Status = "Error", Message = "User update failed! Please check user details and try again." });

        //    return Ok(new StatusResponse { Status = "Success", Message = "User updated successfully!" });
        //}

        //[Authorize]
        //[HttpDelete]
        //[Route("delete")]
        //public async Task<IActionResult> Delete(string username)
        //{
        //    var userToDelete = await userManager.FindByNameAsync(username);
        //    var user = Request.HttpContext.User;

        //    if (userToDelete == null)
        //        return StatusCode(StatusCodes.Status400BadRequest, new StatusResponse { Status = "Error", Message = "" });

        //    if (user.Identity.Name != userToDelete.UserName && user.Claims.Where(s => s.Type == typeSchema).Any(s => s.Value == "Admin") == false)
        //        return StatusCode(StatusCodes.Status401Unauthorized, new StatusResponse { Status = "Error", Message = "Unathorized to delete this user!" });

        //    var result = await userManager.DeleteAsync(userToDelete);
        //    if (!result.Succeeded)
        //        return StatusCode(StatusCodes.Status500InternalServerError, new StatusResponse { Status = "Error", Message = "User deletion failed! Please check user details and try again." });

        //    return Ok(new StatusResponse { Status = "Success", Message = "User deleted successfully!" });
        //}
    }
}
