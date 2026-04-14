using System.ComponentModel.DataAnnotations;

namespace WebHotel.Dtos
{
    public record RoomDto(
        int Id,
        string Number,
        string Type,
        decimal PricePerNight,
        int Capacity,
        string? Description,
        string? ImageUrl);

    public record CreateRoomDto(
        [Required, StringLength(10)] string Number,
        [Required, StringLength(40)] string Type,
        [Required, Range(0.01, 100000)] decimal PricePerNight,
        [Required, Range(1, 10)] int Capacity,
        [StringLength(500)] string? Description,
        [Url] string? ImageUrl);

    public record UpdateRoomDto(
        [Required, StringLength(10)] string Number,
        [Required, StringLength(40)] string Type,
        [Required, Range(0.01, 100000)] decimal PricePerNight,
        [Required, Range(1, 10)] int Capacity,
        [StringLength(500)] string? Description,
        [Url] string? ImageUrl);

    public record AvailabilityQueryDto(
        [Required] DateTime CheckIn,
        [Required] DateTime CheckOut,
        [Range(1, 10)] int Guests = 2);
}
