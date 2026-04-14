using System.ComponentModel.DataAnnotations;
using WebHotel.Models;

namespace WebHotel.Dtos
{
    public record PaymentEntryDto(
        int Id,
        int BookingId,
        PaymentEntryType Type,
        PaymentMethod Method,
        string Description,
        decimal Amount,
        string? Reference,
        string? MaskedCardNumber,
        string? ProcessedBy,
        DateTime ProcessedAt);

    public record BookingStatementDto(
        int BookingId,
        string CustomerName,
        string RoomNumber,
        DateTime CheckIn,
        DateTime CheckOut,
        decimal RoomTotal,
        decimal ExtraCharges,
        decimal PaymentsReceived,
        decimal RefundTotal,
        decimal BalanceDue,
        bool IsPaid,
        IReadOnlyList<PaymentEntryDto> Entries);
}
