using System.ComponentModel.DataAnnotations;

namespace WebHotel.Models
{
    public enum ChatSessionStatus
    {
        Bot = 1,        // Customer is talking to the bot
        Waiting = 2,    // Customer requested staff — waiting in queue
        Active = 3,     // Staff has joined
        Closed = 4      // Session ended
    }

    public enum ChatSenderRole
    {
        Customer = 1,
        Bot = 2,
        Staff = 3,
        System = 4
    }

    public class ChatSession
    {
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        [StringLength(120)]
        public string CustomerName { get; set; } = "";

        public ChatSessionStatus Status { get; set; } = ChatSessionStatus.Bot;

        /// <summary>The Identity user ID of the staff member who picked up the chat.</summary>
        [StringLength(450)]
        public string? StaffUserId { get; set; }

        [StringLength(120)]
        public string? StaffName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }

    public class ChatMessage
    {
        public int Id { get; set; }

        [Required]
        public int ChatSessionId { get; set; }
        public ChatSession? ChatSession { get; set; }

        [Required]
        public ChatSenderRole SenderRole { get; set; }

        [StringLength(120)]
        public string? SenderName { get; set; }

        [Required, StringLength(2000)]
        public string Content { get; set; } = "";

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
