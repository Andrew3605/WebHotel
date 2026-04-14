using System.ComponentModel.DataAnnotations;

namespace WebHotel.Dtos
{
    public record BookingDto(
        int Id,
        int CustomerId,
        string? CustomerName,
        int RoomId,
        string? RoomNumber,
        DateTime CheckIn,
        DateTime CheckOut,
        decimal TotalPrice,
        bool IsPaid,
        decimal BalanceDue,
        DateTime CreatedAt);

    public record CreateBookingDto(
        [Required] int CustomerId,
        [Required] int RoomId,
        [Required] DateTime CheckIn,
        [Required] DateTime CheckOut);

    public record UpdateBookingDto(
        [Required] int CustomerId,
        [Required] int RoomId,
        [Required] DateTime CheckIn,
        [Required] DateTime CheckOut,
        bool IsPaid);
}
