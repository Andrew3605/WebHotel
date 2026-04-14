using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHotel.Data;
using WebHotel.Dtos;
using WebHotel.Models;

namespace WebHotel.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    public class CustomersApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CustomersApiController(AppDbContext context) => _context = context;

        /// <summary>List all customers with optional search.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CustomerDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] string? search)
        {
            var query = _context.Customers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(c =>
                    c.FullName.ToLower().Contains(s) ||
                    c.Email.ToLower().Contains(s) ||
                    (c.Phone != null && c.Phone.Contains(search)));
            }

            var customers = await query
                .OrderBy(c => c.FullName)
                .Select(c => new CustomerDto(c.Id, c.FullName, c.Email, c.Phone))
                .ToListAsync();

            return Ok(customers);
        }

        /// <summary>Get a single customer by ID.</summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(int id)
        {
            var c = await _context.Customers.FindAsync(id);
            if (c == null) return NotFound();

            return Ok(new CustomerDto(c.Id, c.FullName, c.Email, c.Phone));
        }

        /// <summary>Create a new customer (Admin only).</summary>
        [HttpPost]
        [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
        {
            var customer = new Customer
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            var result = new CustomerDto(customer.Id, customer.FullName, customer.Email, customer.Phone);
            return CreatedAtAction(nameof(Get), new { id = customer.Id }, result);
        }

        /// <summary>Update an existing customer (Admin only).</summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerDto dto)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            customer.FullName = dto.FullName;
            customer.Email = dto.Email;
            customer.Phone = dto.Phone;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>Delete a customer (Admin only).</summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
