namespace WebHotel.Services
{
    public interface IChatBotService
    {
        string GetResponse(string userMessage);
    }

    /// <summary>
    /// Rule-based FAQ chat bot. Designed to be swapped with an AI provider
    /// (e.g. OpenAI, Anthropic) by implementing the IChatBotService interface.
    /// </summary>
    public class FaqChatBotService : IChatBotService
    {
        private static readonly (string[] Keywords, string Response)[] FaqEntries =
        {
            (new[] { "book", "reserve", "reservation" },
                "To book a room, use the search bar on the Home page to find available rooms by date and guest count. " +
                "Then click 'Request Booking' on the room you like. Our staff will review and confirm your request."),

            (new[] { "cancel", "cancellation" },
                "To request a cancellation, go to 'My Requests' and submit an Early Checkout request for your booking. " +
                "Our staff will process your request and adjust the booking accordingly."),

            (new[] { "pay", "payment", "deposit", "card", "credit" },
                "You can pay online from 'My Bookings' by clicking the 'Pay' button. We accept card payments. " +
                "A 20% deposit is required for the first payment. You can pay the remaining balance anytime before check-out."),

            (new[] { "check-in", "checkin", "arrive", "arrival" },
                "Check-in is available from 2:00 PM on your arrival date. Please bring a valid photo ID. " +
                "If you need early check-in, submit a request through 'My Requests'."),

            (new[] { "check-out", "checkout", "leave", "departure" },
                "Standard check-out time is 11:00 AM. If you need to check out early, submit an 'Early Checkout' request " +
                "through 'My Requests' and your booking will be adjusted."),

            (new[] { "change", "swap", "switch", "room change", "different room" },
                "To change your room, go to 'My Requests' and submit a 'Change Room' request. Select the room you'd like " +
                "to switch to and our staff will check availability and process the change."),

            (new[] { "extend", "longer", "extra night", "stay longer" },
                "To extend your stay, submit an 'Extend Stay' request through 'My Requests'. Select your new check-out date " +
                "and our staff will check availability and update your booking."),

            (new[] { "food", "room service", "meal", "restaurant", "order" },
                "To order food or room service, submit an 'Order Food' request from 'My Requests' with details of what you'd like. " +
                "Our staff will arrange it for you."),

            (new[] { "wifi", "internet", "password" },
                "Free WiFi is available throughout the hotel. Connect to 'WebHotel-Guest' and use your room number as the password. " +
                "Contact the front desk if you have connection issues."),

            (new[] { "parking", "car", "garage" },
                "We offer complimentary parking for guests. The car park is located at the back of the hotel. " +
                "Please register your vehicle at the front desk upon check-in."),

            (new[] { "pool", "gym", "fitness", "spa" },
                "Our pool is open from 7:00 AM to 10:00 PM. The fitness center is open 24/7 for hotel guests. " +
                "Towels are provided at the pool area. Please present your room key for access."),

            (new[] { "price", "cost", "rate", "how much" },
                "Room rates vary by type: Twin rooms start from $100/night, Deluxe rooms from $150/night, " +
                "and Suites from $300/night. Use the search on the Home page to see current availability and pricing."),

            (new[] { "contact", "phone", "email", "reception", "front desk" },
                "You can reach the front desk 24/7. For urgent requests, please call the reception directly. " +
                "For non-urgent matters, submit a request through 'My Requests' in your account."),

            (new[] { "refund", "money back" },
                "Refunds are processed by our staff through the front desk. If you have a payment concern, " +
                "you can view your payment history in the booking statement or contact reception."),

            (new[] { "hello", "hi", "hey", "help", "assist" },
                "Hello! I'm the WebHotel assistant. I can help you with bookings, payments, check-in/out, " +
                "room changes, food orders, and general hotel information. What would you like to know?"),
        };

        public string GetResponse(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                return "Please type a question and I'll do my best to help!";

            var lower = userMessage.ToLower().Trim();

            // Find the best matching FAQ entry
            var bestMatch = FaqEntries
                .Select(faq => new
                {
                    faq.Response,
                    Score = faq.Keywords.Count(kw => lower.Contains(kw))
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            return bestMatch?.Response
                ?? "I'm not sure about that. You can try asking about bookings, payments, check-in/out, " +
                   "room changes, food orders, WiFi, parking, or hotel facilities. " +
                   "For specific help, please contact the front desk through 'My Requests'.";
        }
    }
}
