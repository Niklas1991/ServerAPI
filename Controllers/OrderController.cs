using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public OrderController(NorthwindContext _context, UserManager<Account> _userManager)
        {
            this.context = _context;
            userManager = _userManager;
        }

        [Authorize(Roles ="VD,Admin,CountryManager")]
        [HttpGet("get-all-orders")]
        public async Task<ActionResult<IEnumerable<OrderResponse>>> GetAllOrders()
        {
            var user = await userManager.FindByNameAsync(HttpContext.User.Identity.Name);

            if (await userManager.IsInRoleAsync(user, Role.CountryManager.ToString()) == true)
			{
                var employee = await context.Employees.Where(x => x.EmployeeId == user.EmployeeId).FirstOrDefaultAsync();
                var orderResult = await context.Orders.Where(x => x.ShipCountry == employee.Country).ToListAsync();
                if (orderResult == null)
				{
                    BadRequest();
                }
                return Ok(orderResult);
			}                     
            var result = await context.Orders.ToListAsync();
			if (result == null)
			{
                BadRequest();
            }
            return Ok(result);			
        }


        [Authorize(Roles = "VD,Admin,CountryManager")]
        [HttpGet("get-country-orders")]
        public async Task<ActionResult<IEnumerable<OrderResponse>>> GetCountryOrders(string country)
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
			}
            if (await userManager.IsInRoleAsync(user, Role.Admin.ToString()) == true || await userManager.IsInRoleAsync(user, Role.VD.ToString()) == true)
            {
                orderResult = await context.Orders.Where(x => x.ShipCountry == country).ToListAsync();
                return Ok(orderResult);
            }
            return Unauthorized();
        }

        [Authorize(Roles = "Employee,Admin,VD")]
        [HttpGet("get-my-orders")]
        public async Task<ActionResult<IEnumerable<OrderResponse>>> GetMyOrders(int employeeId)
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
                return Ok(orderResult);
            }
            if (await userManager.IsInRoleAsync(user, Role.Employee.ToString()))
			{
                orderResult = await context.Orders.Where(x => x.EmployeeId == employee.EmployeeId).ToListAsync();
                if (orderResult == null)
                {
                    return NotFound();
                }
                return Ok(orderResult);
            }
            return Unauthorized();
        }
        

        [HttpPut("{id}")]
        public async Task<IActionResult> PutOrders(int id, Orders orders)
        {
            if (id != orders.OrderId)
            {
                return BadRequest();
            }

            context.Entry(orders).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrdersExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Order
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPost]
        public async Task<ActionResult<Orders>> PostOrders(Orders orders)
        {
            context.Orders.Add(orders);
            await context.SaveChangesAsync();

            return CreatedAtAction("GetOrders", new { id = orders.OrderId }, orders);
        }

        // DELETE: api/Order/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Orders>> DeleteOrders(int id)
        {
            var orders = await context.Orders.FindAsync(id);
            if (orders == null)
            {
                return NotFound();
            }

            context.Orders.Remove(orders);
            await context.SaveChangesAsync();

            return orders;
        }

        private bool OrdersExists(int id)
        {
            return context.Orders.Any(e => e.OrderId == id);
        }
    }
}
