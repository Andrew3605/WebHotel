namespace WebHotel.ViewModels
{
    public class AdminDashboardVm
    {
        // Summary cards
        public int TotalRooms { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalBookings { get; set; }
        public int PendingRequests { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal OutstandingBalance { get; set; }

        // Occupancy: rooms booked today vs total rooms
        public int RoomsOccupiedToday { get; set; }
        public decimal OccupancyRate { get; set; }

        // Chart data: bookings per month (last 6 months)
        public List<string> BookingMonths { get; set; } = new();
        public List<int> BookingCounts { get; set; } = new();

        // Chart data: revenue by room type
        public List<string> RoomTypes { get; set; } = new();
        public List<decimal> RevenueByType { get; set; } = new();

        // Recent bookings
        public List<RecentBookingVm> RecentBookings { get; set; } = new();
    }

    public class RecentBookingVm
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsPaid { get; set; }
    }
}
