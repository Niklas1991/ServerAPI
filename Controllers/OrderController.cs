using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using ServerAPI.Data;
using ServerAPI.Entities;
using ServerAPI.Models;
using ServerAPI.Models.Response;

namespace ServerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly NorthwindContext context;
        private readonly UserManager<Account> userManager;
        private readonly IMapper mapper;

        public OrderController(NorthwindContext _context, UserManager<Account> _userManager, IMapper _mapper)
        {
            this.context = _context;
            userManager = _userManager;
            mapper = _mapper;
        }

        [Authorize(Roles ="VD,Admin,CountryManager")]
        [HttpGet("get-all-orders")]
        public async Task<ActionResult<IEnumerable<OrderResponse>>> GetAllOrders()
        {
            var orderResult = new List<Orders>();
            var user = await userManager.FindByNameAsync(HttpContext.User.Identity.Name);

            if (await userManager.IsInRoleAsync(user, Role.CountryManager.ToString()) == true)
			{
                var employee = await context.Employees.Where(x => x.EmployeeId == user.EmployeeId).FirstOrDefaultAsync();
                orderResult = await context.Orders.Where(x => x.ShipCountry == employee.Country).ToListAsync();
                if (orderResult == null)
				{
                    BadRequest();
                }
                var mappedResult = mapper.Map<IEnumerable<OrderResponse>>(orderResult);
                return Ok(mappedResult);
            }                     
            orderResult = await context.Orders.ToListAsync();
			if (orderResult == null)
			{
                BadRequest();
            }
            var mappedResultAdminVD = mapper.Map<IEnumerable<OrderResponse>>(orderResult);
            return Ok(mappedResultAdminVD);
        }

        [Authorize(Roles = "VD,Admin,CountryManager")]
        [HttpGet("get-country-orders")]
        public async Task<ActionResult<IEnumerable<OrderResponse>>> GetCountryOrders(string? country = null)
        {
            var orderResult = new List<Orders>();
            var user = await userManager.FindByNameAsync(HttpContext.User.Identity.Name);
            if (await userManager.IsInRoleAsync(user, Role.CountryManager.ToString()) == true)  
			{
                var employee = await context.Employees.Where(x => x.EmployeeId == user.EmployeeId).FirstOrDefaultAsync();
                orderResult = await context.Orders.Where(x => x.ShipCountry == employee.Country).ToListAsync();
                if (orderResult == null)
				{
                    return BadRequest();
                }
                var mappedResult = mapper.Map<IEnumerable<OrderResponse>>(orderResult);
                return Ok(mappedResult);
            }
            if (await userManager.IsInRoleAsync(user, Role.Admin.ToString()) == true || await userManager.IsInRoleAsync(user, Role.VD.ToString()) == true)
            {
                orderResult = await context.Orders.Where(x => x.ShipCountry == country).ToListAsync();
                var mappedResult = mapper.Map<IEnumerable<OrderResponse>>(orderResult);
                return Ok(mappedResult);
            }
            return Unauthorized();
        }

        [Authorize(Roles = "Employee,Admin,VD")]
        [HttpGet("get-my-orders")]
        public async Task<ActionResult<IEnumerable<OrderResponse>>> GetMyOrders(int? employeeId = null)
        {
            
            var orderResult = new List<Orders>();
            var user = await userManager.FindByNameAsync(HttpContext.User.Identity.Name);
            var employee = await context.Employees.Where(x => x.EmployeeId == user.EmployeeId).FirstOrDefaultAsync();
            
            if (await userManager.IsInRoleAsync(user, Role.VD.ToString()) == true || await userManager.IsInRoleAsync(user, Role.Admin.ToString())==true)
			{               
                orderResult = await context.Orders.Where(x => x.EmployeeId == employeeId).ToListAsync();
                if (orderResult == null)
                {
                    return NotFound();
                }
                var mappedResult = mapper.Map<IEnumerable<OrderResponse>>(orderResult);
                return Ok(mappedResult);
            }
            if (await userManager.IsInRoleAsync(user, Role.Employee.ToString()))
			{
                orderResult = await context.Orders.Where(x => x.EmployeeId == employee.EmployeeId).ToListAsync();
                if (orderResult == null)
                {
                    return NotFound();
                }
                var mappedResult = mapper.Map<IEnumerable<OrderResponse>>(orderResult);
                return Ok(mappedResult);
            }
            return Unauthorized();
        }    
    }
}
