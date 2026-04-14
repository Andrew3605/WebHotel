using System.ComponentModel.DataAnnotations;

namespace WebHotel.Dtos
{
    public record CustomerDto(
        int Id,
        string FullName,
        string Email,
        string? Phone);

    public record CreateCustomerDto(
        [Required, StringLength(80)] string FullName,
        [Required, EmailAddress, StringLength(120)] string Email,
        [Phone, StringLength(40)] string? Phone);

    public record UpdateCustomerDto(
        [Required, StringLength(80)] string FullName,
        [Required, EmailAddress, StringLength(120)] string Email,
        [Phone, StringLength(40)] string? Phone);
}
