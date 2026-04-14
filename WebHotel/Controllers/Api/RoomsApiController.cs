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
    [Produces("application/json")]
    public class RoomsApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        public RoomsApiController(AppDbContext context) => _context = context;

        /// <summary>List all rooms, optionally filtered by search term.</summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<RoomDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] string? search)
        {
            var query = _context.Rooms.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(r =>
                    r.Number.ToLower().Contains(s) ||
                    r.Type.ToLower().Contains(s));
            }

            var rooms = await query.OrderBy(r => r.Number)
                .Select(r => new RoomDto(r.Id, r.Number, r.Type, r.PricePerNight,
                    r.Capacity, r.Description, r.ImageUrl))
                .ToListAsync();

            return Ok(rooms);
        }

        /// <summary>Get a single room by ID.</summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RoomDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(int id)
        {
            var r = await _context.Rooms.FindAsync(id);
            if (r == null) return NotFound();

            return Ok(new RoomDto(r.Id, r.Number, r.Type, r.PricePerNight,
                r.Capacity, r.Description, r.ImageUrl));
        }

        /// <summary>Search available rooms for given dates and guest count.</summary>
        [HttpGet("availability")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<RoomDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Availability([FromQuery] AvailabilityQueryDto q)
        {
            if (q.CheckOut <= q.CheckIn)
                return BadRequest(new { error = "Check-out must be after check-in." });

            var bookedRoomIds = await _context.Bookings
                .Where(b => !(b.CheckOut <= q.CheckIn || b.CheckIn >= q.CheckOut))
                .Select(b => b.RoomId)
                .Distinct()
                .ToListAsync();

            var rooms = await _context.Rooms
                .Where(r => !bookedRoomIds.Contains(r.Id) && r.Capacity >= q.Guests)
                .OrderBy(r => r.PricePerNight)
                .Select(r => new RoomDto(r.Id, r.Number, r.Type, r.PricePerNight,
                    r.Capacity, r.Description, r.ImageUrl))
                .ToListAsync();

            return Ok(rooms);
        }

        /// <summary>Create a new room (Admin only).</summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(RoomDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateRoomDto dto)
        {
            var room = new Room
            {
                Number = dto.Number,
                Type = dto.Type,
                PricePerNight = dto.PricePerNight,
                Capacity = dto.Capacity,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl
            };

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            var result = new RoomDto(room.Id, room.Number, room.Type, room.PricePerNight,
                room.Capacity, room.Description, room.ImageUrl);

            return CreatedAtAction(nameof(Get), new { id = room.Id }, result);
        }

        /// <summary>Update an existing room (Admin only).</summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRoomDto dto)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            room.Number = dto.Number;
            room.Type = dto.Type;
            room.PricePerNight = dto.PricePerNight;
            room.Capacity = dto.Capacity;
            room.Description = dto.Description;
            room.ImageUrl = dto.ImageUrl;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>Delete a room (Admin only).</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
