namespace WebHotel.Services
{
    public interface IHotelEmailSender
    {
        Task SendBookingConfirmationAsync(string toEmail, string customerName,
            int bookingId, string roomNumber, DateTime checkIn, DateTime checkOut, decimal totalPrice);

        Task SendPaymentReceiptAsync(string toEmail, string customerName,
            int bookingId, decimal amount, decimal balanceDue);

        Task SendRequestStatusAsync(string toEmail, string customerName,
            string requestType, string status, string? adminNote = null);
    }
}
