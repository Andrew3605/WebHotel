using System.ComponentModel.DataAnnotations;
using WebHotel.Models;
using System.Collections.Generic;

namespace WebHotel.ViewModels
{
    public class AvailabilitySearchVm
    {
        [DataType(DataType.Date)]
        [Required]
        public DateTime CheckIn { get; set; }

        [DataType(DataType.Date)]
        [Required]
        public DateTime CheckOut { get; set; }

        [Range(1, 10)]
        public int Guests { get; set; } = 2;

        // Results
        public List<Room> AvailableRooms { get; set; } = new();
    }
}
