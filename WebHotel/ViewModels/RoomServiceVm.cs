using WebHotel.Models;

namespace WebHotel.ViewModels
{
    public class RoomServiceVm
    {
        public int BookingId { get; set; }
        public string RoomNumber { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public decimal BalanceDue { get; set; }

        public string? CategoryFilter { get; set; }
        public List<string> Categories { get; set; } = new();
        public List<Product> Products { get; set; } = new();
    }

    public class RoomServiceOrderVm
    {
        public int BookingId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
        public string? Note { get; set; }
    }
}
