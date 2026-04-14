using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHotel.Data;
using WebHotel.Dtos;
using WebHotel.Services;

namespace WebHotel.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    public class PaymentsApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        public PaymentsApiController(AppDbContext context) => _context = context;

        /// <summary>Get all payment entries for a booking.</summary>
        [HttpGet("booking/{bookingId}")]
        [ProducesResponseType(typeof(IEnumerable<PaymentEntryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByBooking(int bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) return NotFound();

            var entries = await _context.PaymentEntries
                .Where(e => e.BookingId == bookingId)
                .OrderBy(e => e.ProcessedAt)
                .Select(e => new PaymentEntryDto(e.Id, e.BookingId, e.Type, e.Method,
                    e.Description, e.Amount, e.Reference, e.MaskedCardNumber,
                    e.ProcessedBy, e.ProcessedAt))
                .ToListAsync();

            return Ok(entries);
        }

        /// <summary>Get a booking's financial summary.</summary>
        [HttpGet("booking/{bookingId}/summary")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Summary(int bookingId)
        {
            var b = await _context.Bookings
                .Include(b => b.PaymentEntries)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (b == null) return NotFound();

            return Ok(new
            {
                BookingId = b.Id,
                RoomTotal = b.TotalPrice,
                ExtraCharges = BookingPaymentCalculator.GetExtraCharges(b.PaymentEntries),
                PaymentsReceived = BookingPaymentCalculator.GetPaymentsReceived(b.PaymentEntries),
                RefundTotal = BookingPaymentCalculator.GetRefundTotal(b.PaymentEntries),
                BalanceDue = BookingPaymentCalculator.GetBalanceDue(b.TotalPrice, b.PaymentEntries),
                IsFullyPaid = BookingPaymentCalculator.IsFullyPaid(b.TotalPrice, b.PaymentEntries)
            });
        }
    }
}
