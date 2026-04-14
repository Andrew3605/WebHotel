using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WebHotel.Data;
using WebHotel.Models;
using WebHotel.Services;

namespace WebHotel.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly IChatBotService _chatBot;

        public ChatHub(AppDbContext db, UserManager<ApplicationUser> users, IChatBotService chatBot)
        {
            _db = db;
            _users = users;
            _chatBot = chatBot;
        }

        // ─── Customer methods ───────────────────────────

        /// <summary>Customer opens the chat widget — find or create a session.</summary>
        [Authorize(Roles = "Customer")]
        public async Task<int> StartSession()
        {
            var user = await _users.GetUserAsync(Context.User!);
            if (user?.CustomerId == null) throw new HubException("Not a customer.");

            // Reuse an open session if one exists
            var session = await _db.ChatSessions
                .Include(s => s.Messages)
                .Where(s => s.CustomerId == user.CustomerId.Value && s.Status != ChatSessionStatus.Closed)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            if (session == null)
            {
                var customer = await _db.Customers.FindAsync(user.CustomerId.Value);
                session = new ChatSession
                {
                    CustomerId = user.CustomerId.Value,
                    CustomerName = customer?.FullName ?? user.Email ?? "Guest",
                    Status = ChatSessionStatus.Bot,
                    CreatedAt = DateTime.UtcNow
                };
                _db.ChatSessions.Add(session);
                await _db.SaveChangesAsync();
            }

            // Join the SignalR group for this session
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat-{session.Id}");

            return session.Id;
        }

        /// <summary>Customer sends a message.</summary>
        [Authorize(Roles = "Customer")]
        public async Task SendCustomerMessage(int sessionId, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            message = message.Trim();
            if (message.Length > 2000) message = message[..2000];

            var user = await _users.GetUserAsync(Context.User!);
            if (user?.CustomerId == null) return;

            var session = await _db.ChatSessions.FindAsync(sessionId);
            if (session == null || session.CustomerId != user.CustomerId.Value) return;
            if (session.Status == ChatSessionStatus.Closed) return;

            // Save customer message
            var customerMsg = new ChatMessage
            {
                ChatSessionId = sessionId,
                SenderRole = ChatSenderRole.Customer,
                SenderName = session.CustomerName,
                Content = message,
                SentAt = DateTime.UtcNow
            };
            _db.ChatMessages.Add(customerMsg);
            await _db.SaveChangesAsync();

            // Broadcast to the session group (so staff sees it too)
            await Clients.Group($"chat-{sessionId}").SendAsync("ReceiveMessage", new
            {
                senderRole = "Customer",
                senderName = session.CustomerName,
                content = message,
                sentAt = customerMsg.SentAt
            });

            // If still in bot mode, auto-reply
            if (session.Status == ChatSessionStatus.Bot)
            {
                var botReply = _chatBot.GetResponse(message);
                var botMsg = new ChatMessage
                {
                    ChatSessionId = sessionId,
                    SenderRole = ChatSenderRole.Bot,
                    SenderName = "Hotel Assistant",
                    Content = botReply,
                    SentAt = DateTime.UtcNow
                };
                _db.ChatMessages.Add(botMsg);
                await _db.SaveChangesAsync();

                await Clients.Group($"chat-{sessionId}").SendAsync("ReceiveMessage", new
                {
                    senderRole = "Bot",
                    senderName = "Hotel Assistant",
                    content = botReply,
                    sentAt = botMsg.SentAt
                });
            }
        }

        /// <summary>Customer requests escalation to a staff member.</summary>
        [Authorize(Roles = "Customer")]
        public async Task RequestStaff(int sessionId)
        {
            var user = await _users.GetUserAsync(Context.User!);
            if (user?.CustomerId == null) return;

            var session = await _db.ChatSessions.FindAsync(sessionId);
            if (session == null || session.CustomerId != user.CustomerId.Value) return;
            if (session.Status != ChatSessionStatus.Bot) return; // Already escalated

            session.Status = ChatSessionStatus.Waiting;

            var sysMsg = new ChatMessage
            {
                ChatSessionId = sessionId,
                SenderRole = ChatSenderRole.System,
                SenderName = "System",
                Content = "You've been placed in the queue. A staff member will join shortly.",
                SentAt = DateTime.UtcNow
            };
            _db.ChatMessages.Add(sysMsg);
            await _db.SaveChangesAsync();

            await Clients.Group($"chat-{sessionId}").SendAsync("ReceiveMessage", new
            {
                senderRole = "System",
                senderName = "System",
                content = sysMsg.Content,
                sentAt = sysMsg.SentAt
            });

            // Notify staff dashboard that a new chat is waiting
            await Clients.Group("staff-chat-dashboard").SendAsync("NewChatWaiting", new
            {
                sessionId = session.Id,
                customerName = session.CustomerName,
                createdAt = session.CreatedAt
            });
        }

        /// <summary>Customer closes the chat.</summary>
        [Authorize(Roles = "Customer")]
        public async Task CloseSession(int sessionId)
        {
            var user = await _users.GetUserAsync(Context.User!);
            if (user?.CustomerId == null) return;

            var session = await _db.ChatSessions.FindAsync(sessionId);
            if (session == null || session.CustomerId != user.CustomerId.Value) return;

            session.Status = ChatSessionStatus.Closed;
            session.ClosedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await Clients.Group($"chat-{sessionId}").SendAsync("SessionClosed", sessionId);
        }

        // ─── Staff methods ──────────────────────────────

        /// <summary>Staff joins the dashboard to see waiting chats.</summary>
        [Authorize(Roles = "Admin,Staff")]
        public async Task JoinStaffDashboard()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "staff-chat-dashboard");
        }

        /// <summary>Staff picks up a waiting chat session.</summary>
        [Authorize(Roles = "Admin,Staff")]
        public async Task JoinChat(int sessionId)
        {
            var user = await _users.GetUserAsync(Context.User!);
            if (user == null) return;

            var session = await _db.ChatSessions.FindAsync(sessionId);
            if (session == null) return;
            if (session.Status == ChatSessionStatus.Closed) return;

            // Mark as active with this staff member
            session.Status = ChatSessionStatus.Active;
            session.StaffUserId = user.Id;
            session.StaffName = user.Email ?? user.UserName ?? "Staff";

            var sysMsg = new ChatMessage
            {
                ChatSessionId = sessionId,
                SenderRole = ChatSenderRole.System,
                SenderName = "System",
                Content = $"{session.StaffName} has joined the chat.",
                SentAt = DateTime.UtcNow
            };
            _db.ChatMessages.Add(sysMsg);
            await _db.SaveChangesAsync();

            // Join the group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat-{sessionId}");

            await Clients.Group($"chat-{sessionId}").SendAsync("StaffJoined", new
            {
                staffName = session.StaffName,
                sessionId
            });

            await Clients.Group($"chat-{sessionId}").SendAsync("ReceiveMessage", new
            {
                senderRole = "System",
                senderName = "System",
                content = sysMsg.Content,
                sentAt = sysMsg.SentAt
            });

            // Notify other staff that this chat was picked up
            await Clients.Group("staff-chat-dashboard").SendAsync("ChatPickedUp", sessionId);
        }

        /// <summary>Staff sends a reply in a live chat.</summary>
        [Authorize(Roles = "Admin,Staff")]
        public async Task SendStaffMessage(int sessionId, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            message = message.Trim();
            if (message.Length > 2000) message = message[..2000];

            var user = await _users.GetUserAsync(Context.User!);
            if (user == null) return;

            var session = await _db.ChatSessions.FindAsync(sessionId);
            if (session == null || session.Status != ChatSessionStatus.Active) return;

            var staffMsg = new ChatMessage
            {
                ChatSessionId = sessionId,
                SenderRole = ChatSenderRole.Staff,
                SenderName = user.Email ?? user.UserName ?? "Staff",
                Content = message,
                SentAt = DateTime.UtcNow
            };
            _db.ChatMessages.Add(staffMsg);
            await _db.SaveChangesAsync();

            await Clients.Group($"chat-{sessionId}").SendAsync("ReceiveMessage", new
            {
                senderRole = "Staff",
                senderName = staffMsg.SenderName,
                content = message,
                sentAt = staffMsg.SentAt
            });
        }

        /// <summary>Staff closes a chat session.</summary>
        [Authorize(Roles = "Admin,Staff")]
        public async Task StaffCloseSession(int sessionId)
        {
            var session = await _db.ChatSessions.FindAsync(sessionId);
            if (session == null) return;

            session.Status = ChatSessionStatus.Closed;
            session.ClosedAt = DateTime.UtcNow;

            var sysMsg = new ChatMessage
            {
                ChatSessionId = sessionId,
                SenderRole = ChatSenderRole.System,
                SenderName = "System",
                Content = "This chat has been closed by staff. Feel free to start a new chat anytime.",
                SentAt = DateTime.UtcNow
            };
            _db.ChatMessages.Add(sysMsg);
            await _db.SaveChangesAsync();

            await Clients.Group($"chat-{sessionId}").SendAsync("ReceiveMessage", new
            {
                senderRole = "System",
                senderName = "System",
                content = sysMsg.Content,
                sentAt = sysMsg.SentAt
            });
            await Clients.Group($"chat-{sessionId}").SendAsync("SessionClosed", sessionId);
        }

        // ─── Connection management ──────────────────────

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
