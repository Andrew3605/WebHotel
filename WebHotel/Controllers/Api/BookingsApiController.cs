using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHotel.Data;
using WebHotel.Dtos;
using WebHotel.Models;
using WebHotel.Services;

namespace WebHotel.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    public class BookingsApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        public BookingsApiController(AppDbContext context) => _context = context;

        /// <summary>List all bookings with optional search.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<BookingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] string? search)
        {
            var query = _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .Include(b => b.PaymentEntries)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(b =>
                    b.Id.ToString().Contains(search) ||
                    (b.Customer != null && b.Customer.FullName.ToLower().Contains(s)) ||
                    (b.Room != null && b.Room.Number.ToLower().Contains(s)) ||
                    (s == "paid" && b.IsPaid) ||
                    (s == "unpaid" && !b.IsPaid));
            }

            var bookings = await query
                .OrderByDescending(b => b.CheckIn)
                .Select(b => new BookingDto(
                    b.Id, b.CustomerId, b.Customer!.FullName,
                    b.RoomId, b.Room!.Number,
                    b.CheckIn, b.CheckOut, b.TotalPrice, b.IsPaid,
                    BookingPaymentCalculator.GetBalanceDue(b.TotalPrice, b.PaymentEntries),
                    b.CreatedAt))
                .ToListAsync();

            return Ok(bookings);
        }

        /// <summary>Get a single booking by ID.</summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(int id)
        {
            var b = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .Include(b => b.PaymentEntries)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (b == null) return NotFound();

            return Ok(new BookingDto(
                b.Id, b.CustomerId, b.Customer?.FullName,
                b.RoomId, b.Room?.Number,
                b.CheckIn, b.CheckOut, b.TotalPrice, b.IsPaid,
                b.BalanceDue, b.CreatedAt));
        }

        /// <summary>Get the payment statement for a booking.</summary>
        [HttpGet("{id}/statement")]
        [ProducesResponseType(typeof(BookingStatementDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Statement(int id)
        {
            var b = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .Include(b => b.PaymentEntries)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (b == null) return NotFound();

            var entries = b.PaymentEntries
                .OrderBy(e => e.ProcessedAt)
                .Select(e => new PaymentEntryDto(e.Id, e.BookingId, e.Type, e.Method,
                    e.Description, e.Amount, e.Reference, e.MaskedCardNumber,
                    e.ProcessedBy, e.ProcessedAt))
                .ToList();

            return Ok(new BookingStatementDto(
                b.Id, b.Customer?.FullName ?? "", b.Room?.Number ?? "",
                b.CheckIn, b.CheckOut, b.TotalPrice,
                b.ExtraCharges, b.PaymentsReceived, b.RefundTotal,
                b.BalanceDue, b.IsPaid, entries));
        }

        /// <summary>Create a new booking (Admin only).</summary>
        [HttpPost]
        [ProducesResponseType(typeof(BookingDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateBookingDto dto)
        {
            if (dto.CheckOut <= dto.CheckIn)
                return BadRequest(new { error = "Check-out must be after check-in." });

            bool conflict = await _context.Bookings.AnyAsync(b =>
                b.RoomId == dto.RoomId &&
                !(b.CheckOut <= dto.CheckIn || b.CheckIn >= dto.CheckOut));

            if (conflict)
                return BadRequest(new { error = "Room is not available for those dates." });

            var room = await _context.Rooms.FindAsync(dto.RoomId);
            if (room == null) return BadRequest(new { error = "Room not found." });

            var customer = await _context.Customers.FindAsync(dto.CustomerId);
            if (customer == null) return BadRequest(new { error = "Customer not found." });

            var nights = Math.Max(1, (dto.CheckOut.Date - dto.CheckIn.Date).Days);
            var booking = new Booking
            {
                CustomerId = dto.CustomerId,
                RoomId = dto.RoomId,
                CheckIn = dto.CheckIn.Date,
                CheckOut = dto.CheckOut.Date,
                TotalPrice = room.PricePerNight * nights,
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            var result = new BookingDto(booking.Id, booking.CustomerId, customer.FullName,
                booking.RoomId, room.Number, booking.CheckIn, booking.CheckOut,
                booking.TotalPrice, booking.IsPaid, booking.TotalPrice, booking.CreatedAt);

            return CreatedAtAction(nameof(Get), new { id = booking.Id }, result);
        }

        /// <summary>Update a booking (Admin only).</summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateBookingDto dto)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            if (dto.CheckOut <= dto.CheckIn)
                return BadRequest(new { error = "Check-out must be after check-in." });

            bool conflict = await _context.Bookings.AnyAsync(b =>
                b.RoomId == dto.RoomId && b.Id != id &&
                !(b.CheckOut <= dto.CheckIn || b.CheckIn >= dto.CheckOut));

            if (conflict)
                return BadRequest(new { error = "Room is not available for those dates." });

            var room = await _context.Rooms.FindAsync(dto.RoomId);
            if (room == null) return BadRequest(new { error = "Room not found." });

            var nights = Math.Max(1, (dto.CheckOut.Date - dto.CheckIn.Date).Days);
            booking.CustomerId = dto.CustomerId;
            booking.RoomId = dto.RoomId;
            booking.CheckIn = dto.CheckIn.Date;
            booking.CheckOut = dto.CheckOut.Date;
            booking.TotalPrice = room.PricePerNight * nights;
            booking.IsPaid = dto.IsPaid;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>Delete a booking (Admin only).</summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
