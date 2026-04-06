namespace WebHotel.Models
{
    public enum RequestType { NewBooking, ExtendStay, ChangeRoom, EarlyCheckout, OrderFood }
    public enum RequestStatus { Pending, Approved, Rejected }

    public class CustomerRequest
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }      // from ApplicationUser.CustomerId if present
        public int? RoomId { get; set; }          // for NewBooking and ChangeRoom
        public int? BookingId { get; set; }       // for ExtendStay/ChangeRoom
        public DateTime? CheckIn { get; set; }    // for NewBooking/ExtendStay
        public DateTime? CheckOut { get; set; }   // for NewBooking/ExtendStay
        public RequestType Type { get; set; } = RequestType.NewBooking;
        public string? Message { get; set; }
        public RequestStatus Status { get; set; } = RequestStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
