namespace WebHotel.Services
{
    public interface IChatBotService
    {
        string GetResponse(string userMessage);
    }

    public class FaqChatBotService : IChatBotService
    {
        private static readonly FaqEntry[] FaqEntries =
        [
            // ── Account & Password ──────────────────────────────────────────────
            new(
                phrases: ["reset password", "forgot password", "change password", "change my password",
                          "reset my password", "forgot my password", "lost my password", "cant log in",
                          "can't log in", "locked out", "cant login", "can't login", "login problem",
                          "account password", "update password", "reset user password", "reset my user password",
                          "how to reset password", "how do i reset", "how do i change my password"],
                keywords: ["credentials", "reset", "password"],
                response: "To change your password, click your name in the top-right corner and select 'Manage Profile'. " +
                          "From there go to the Security section and you can update your password.\n\n" +
                          "If you're locked out or forgot your password, click 'Forgot Password' on the Login page and follow the instructions. " +
                          "Accounts are locked for 15 minutes after 5 failed attempts — after that you can try again or contact the front desk."
            ),

            new(
                phrases: ["change my email", "update email", "change email", "wrong email", "update my email"],
                keywords: [],
                response: "To update your email address, go to 'Manage Profile' (click your name top-right) and look for the Email section. " +
                          "If you can't change it yourself, contact the front desk and an admin can update it for you."
            ),

            new(
                phrases: ["delete my account", "close my account", "remove my account"],
                keywords: [],
                response: "To close or delete your account, please contact the front desk directly. Staff or admin will handle the request " +
                          "and make sure any active bookings are resolved first."
            ),

            // ── Bookings ────────────────────────────────────────────────────────
            new(
                phrases: ["how to book", "how do i book", "book a room", "make a booking", "make a reservation",
                          "new booking", "new reservation", "i want to book", "i want a room"],
                keywords: ["book", "reserve", "reservation", "booking"],
                response: "Booking a room is easy:\n" +
                          "1. Go to the Home page\n" +
                          "2. Enter your check-in date, check-out date, and number of guests\n" +
                          "3. Click 'Search' to see available rooms\n" +
                          "4. Click 'Request Booking' on the room you want\n\n" +
                          "Our staff will review and confirm your booking — you'll be notified by email once it's approved."
            ),

            new(
                phrases: ["view my booking", "see my booking", "check my booking", "my bookings", "booking status",
                          "booking confirmation", "is my booking confirmed", "booking details"],
                keywords: [],
                response: "You can view all your bookings by clicking 'My Bookings' in the navigation menu. " +
                          "There you'll see the status (Pending, Confirmed, etc.), dates, room details, and payment info. " +
                          "If your booking says Pending, it means staff are reviewing it — usually within a few hours."
            ),

            new(
                phrases: ["cancel booking", "cancel my booking", "cancel reservation", "cancel my stay",
                          "cancel my room", "i want to cancel"],
                keywords: ["cancel", "cancellation"],
                response: "To cancel your stay:\n" +
                          "1. Go to 'My Requests'\n" +
                          "2. Submit an 'Early Checkout' request for your booking\n" +
                          "3. Staff will review and process the cancellation\n\n" +
                          "Please note: refund eligibility depends on how far in advance you cancel. " +
                          "Contact the front desk if you need urgent cancellation assistance."
            ),

            new(
                phrases: ["extend stay", "stay longer", "extra night", "add nights", "extend my stay",
                          "i want to stay longer", "can i stay an extra night"],
                keywords: ["extend", "longer"],
                response: "To extend your stay:\n" +
                          "1. Go to 'My Requests'\n" +
                          "2. Submit an 'Extend Stay' request\n" +
                          "3. Choose your new check-out date\n\n" +
                          "Staff will check if the room is available for the extra nights and update your booking. " +
                          "Note that the extra nights will be added to your payment."
            ),

            new(
                phrases: ["change room", "switch room", "different room", "upgrade room", "move room",
                          "i don't like my room", "i dont like my room", "can i change room",
                          "can i switch room", "room upgrade"],
                keywords: ["upgrade"],
                response: "To request a room change or upgrade:\n" +
                          "1. Go to 'My Requests'\n" +
                          "2. Submit a 'Change Room' request\n" +
                          "3. Select the room you'd prefer\n\n" +
                          "Staff will check availability and confirm the change. If the new room has a different rate, " +
                          "the price difference will be applied to your booking."
            ),

            // ── Payments ────────────────────────────────────────────────────────
            new(
                phrases: ["how to pay", "make a payment", "pay my bill", "pay online", "pay for my booking",
                          "how do i pay", "i want to pay", "online payment"],
                keywords: ["pay", "payment", "invoice", "bill"],
                response: "To pay for your booking:\n" +
                          "1. Go to 'My Bookings'\n" +
                          "2. Find your booking and click the 'Pay' button\n" +
                          "3. Follow the payment steps\n\n" +
                          "We accept card payments. A 20% deposit is required upfront, and the remaining balance can be paid " +
                          "any time before check-out."
            ),

            new(
                phrases: ["payment history", "payment receipt", "view payment", "see my payments",
                          "payment statement", "invoice history", "paid already", "already paid"],
                keywords: ["receipt", "statement"],
                response: "To view your payment history and receipts:\n" +
                          "1. Go to 'My Bookings'\n" +
                          "2. Click 'Statement' on your booking\n\n" +
                          "This shows all payments made, amounts, and dates. You can also print or save it from there."
            ),

            new(
                phrases: ["deposit amount", "how much deposit", "deposit required", "how much do i pay upfront"],
                keywords: ["deposit"],
                response: "A 20% deposit of the total booking cost is required as the first payment. " +
                          "The remaining 80% can be paid any time before check-out. " +
                          "You can make multiple payments — it doesn't have to be all at once."
            ),

            new(
                phrases: ["refund request", "get a refund", "money back", "refund my payment",
                          "i want a refund", "can i get refund"],
                keywords: ["refund"],
                response: "Refund requests are handled by our staff. To request one:\n" +
                          "1. Go to 'My Requests' and submit a request with details of what you'd like refunded\n" +
                          "2. Or contact the front desk directly\n\n" +
                          "Refund eligibility depends on your booking dates and cancellation timing. " +
                          "Staff will review and get back to you."
            ),

            new(
                phrases: ["payment failed", "card declined", "payment not working", "cant pay", "can't pay",
                          "payment error", "payment issue"],
                keywords: [],
                response: "If your payment isn't going through, try these steps:\n" +
                          "• Make sure your card details are entered correctly\n" +
                          "• Check with your bank that online payments are enabled\n" +
                          "• Try a different card or payment method\n\n" +
                          "If the issue persists, contact the front desk and staff can assist with alternative payment options."
            ),

            // ── Check-in / Check-out ─────────────────────────────────────────────
            new(
                phrases: ["check in time", "check-in time", "when can i check in", "early check in",
                          "what time check in", "arrival time", "when do i arrive"],
                keywords: ["checkin", "arrive", "arrival"],
                response: "Standard check-in is from 2:00 PM on your arrival date.\n\n" +
                          "Need to arrive earlier? Submit an 'Early Check-in' request through 'My Requests' " +
                          "and staff will do their best to have your room ready. Please bring a valid photo ID when you arrive."
            ),

            new(
                phrases: ["check out time", "check-out time", "when do i check out", "late check out",
                          "what time check out", "departure time", "when do i leave"],
                keywords: ["checkout", "leave", "departure"],
                response: "Standard check-out is 11:00 AM on your departure date.\n\n" +
                          "Need more time? Submit a 'Late Check-out' request through 'My Requests' and staff will check " +
                          "if an extension is available. Late check-out may incur an additional charge depending on availability."
            ),

            new(
                phrases: ["luggage storage", "store my bags", "leave my bags", "bag storage", "store luggage"],
                keywords: ["luggage", "bags", "suitcase"],
                response: "We offer complimentary luggage storage at the front desk. You can drop off your bags before check-in " +
                          "or after check-out while you explore. Just speak to the front desk staff to arrange this."
            ),

            // ── Room Information ─────────────────────────────────────────────────
            new(
                phrases: ["what rooms do you have", "types of rooms", "room types", "available rooms",
                          "what rooms are available", "room options"],
                keywords: ["rooms", "room"],
                response: "We offer several room types:\n" +
                          "• **Twin Room** — from $100/night, ideal for 1–2 guests\n" +
                          "• **Deluxe Room** — from $150/night, more space and premium amenities\n" +
                          "• **Suite** — from $300/night, our most spacious option with luxury features\n\n" +
                          "Use the search on the Home page to see which rooms are available for your dates."
            ),

            new(
                phrases: ["room amenities", "what is included", "whats included", "what does the room include",
                          "room facilities", "what do i get in the room"],
                keywords: ["amenities", "included", "facilities"],
                response: "All our rooms include:\n" +
                          "• Free WiFi\n• Air conditioning\n• Private bathroom\n• Flat-screen TV\n" +
                          "• Daily housekeeping\n• Tea & coffee making facilities\n\n" +
                          "Deluxe rooms and Suites also include a minibar, premium toiletries, and a larger seating area. " +
                          "Suites have a separate living room and upgraded furnishings."
            ),

            new(
                phrases: ["how many guests", "how many people", "room capacity", "max guests", "maximum guests",
                          "can i bring extra person", "extra bed", "cot", "baby cot"],
                keywords: ["guests", "capacity", "person", "people"],
                response: "Room capacity varies by type:\n" +
                          "• Twin Room: up to 2 guests\n• Deluxe Room: up to 2 guests\n• Suite: up to 4 guests\n\n" +
                          "If you need a cot or extra bedding for a child, submit a request through 'My Requests' " +
                          "and staff will arrange it before your arrival."
            ),

            new(
                phrases: ["pet friendly", "can i bring my dog", "can i bring my cat", "pets allowed", "bring a pet"],
                keywords: ["pet", "dog", "cat", "animal"],
                response: "We do not currently accept pets at WebHotel, with the exception of registered assistance/service animals. " +
                          "If you have an assistance animal, please notify the front desk in advance so we can make appropriate arrangements."
            ),

            new(
                phrases: ["smoking room", "can i smoke", "is smoking allowed", "smoke in room"],
                keywords: ["smoke", "smoking", "cigarette"],
                response: "WebHotel is a non-smoking property. Smoking is not permitted in any rooms or indoor areas. " +
                          "A designated outdoor smoking area is available near the main entrance. " +
                          "A cleaning fee will be charged if smoking is detected in a room."
            ),

            // ── Food & Room Service ──────────────────────────────────────────────
            new(
                phrases: ["order food", "room service", "food delivery", "order to room", "food to my room",
                          "i want food", "hungry", "can i order food", "food menu"],
                keywords: ["food", "meal", "eat", "dinner", "lunch", "breakfast", "snack", "drink"],
                response: "To order food or room service:\n" +
                          "1. Go to 'My Requests'\n" +
                          "2. Submit an 'Order Food' request\n" +
                          "3. Describe what you'd like (meal, drinks, snacks, any dietary needs)\n\n" +
                          "Room service is available from 7:00 AM to 11:00 PM. For late-night requests, contact the front desk directly."
            ),

            new(
                phrases: ["breakfast time", "what time is breakfast", "breakfast included", "is breakfast free",
                          "morning food"],
                keywords: ["breakfast"],
                response: "Breakfast is served in the hotel restaurant from 7:00 AM to 10:30 AM. " +
                          "Whether breakfast is included depends on your booking package — check your booking details in 'My Bookings'. " +
                          "If not included, breakfast can be added for an additional charge — ask at the front desk."
            ),

            new(
                phrases: ["restaurant hours", "dining hours", "when is restaurant open", "hotel restaurant"],
                keywords: ["restaurant", "dining"],
                response: "The hotel restaurant is open:\n" +
                          "• Breakfast: 7:00 AM – 10:30 AM\n" +
                          "• Lunch: 12:00 PM – 3:00 PM\n" +
                          "• Dinner: 6:00 PM – 10:00 PM\n\n" +
                          "Room service is available 7:00 AM – 11:00 PM. For out-of-hours food, contact the front desk."
            ),

            new(
                phrases: ["dietary requirements", "vegetarian", "vegan", "gluten free", "food allergy",
                          "halal", "kosher", "special diet"],
                keywords: ["dietary", "allergy", "vegetarian", "vegan", "halal"],
                response: "We cater to various dietary requirements including vegetarian, vegan, gluten-free, halal, and more. " +
                          "When submitting a food order through 'My Requests', please mention your dietary needs. " +
                          "For allergies, please also inform the front desk so we can ensure meals are prepared safely."
            ),

            // ── WiFi & Tech ──────────────────────────────────────────────────────
            new(
                phrases: ["wifi password", "wifi network", "connect to wifi", "guest wifi", "internet password",
                          "how do i connect", "wireless password"],
                keywords: ["wifi", "internet", "wireless", "network", "connection"],
                response: "Free WiFi is available throughout the hotel.\n\n" +
                          "• Network name: **WebHotel-Guest**\n" +
                          "• Password: your room number (e.g. 204)\n\n" +
                          "If you have trouble connecting, restart your device and try again. " +
                          "Still having issues? Call the front desk and our team will sort it out for you."
            ),

            new(
                phrases: ["tv not working", "tv broken", "no signal tv", "air conditioning not working",
                          "ac not working", "heating not working", "lights not working", "something broken",
                          "room issue", "maintenance", "something wrong in room"],
                keywords: ["broken", "not working", "issue", "problem", "repair"],
                response: "Sorry to hear something isn't working in your room! Please report it right away:\n" +
                          "1. Submit a request through 'My Requests' describing the issue\n" +
                          "2. Or call the front desk directly for urgent problems\n\n" +
                          "Our maintenance team will attend to it as quickly as possible. " +
                          "For urgent issues (no hot water, heating, etc.) please call the front desk immediately."
            ),

            // ── Hotel Facilities ─────────────────────────────────────────────────
            new(
                phrases: ["swimming pool", "pool hours", "when does pool open", "pool access"],
                keywords: ["pool", "swim", "swimming"],
                response: "The pool is open daily from 7:00 AM to 10:00 PM.\n" +
                          "• Towels are provided poolside — no need to bring your own\n" +
                          "• Show your room key for access\n" +
                          "• Children under 14 must be accompanied by an adult"
            ),

            new(
                phrases: ["gym hours", "fitness center hours", "when is gym open", "gym access", "workout"],
                keywords: ["gym", "fitness", "workout", "exercise"],
                response: "The fitness center is open 24/7 for hotel guests — use your room key to access it. " +
                          "It's equipped with cardio machines, free weights, and resistance equipment. " +
                          "Towels and water are available inside."
            ),

            new(
                phrases: ["spa treatment", "book spa", "spa hours", "massage", "spa access"],
                keywords: ["spa", "massage", "treatment"],
                response: "Our spa offers a range of treatments including massages, facials, and body treatments. " +
                          "Spa hours are 9:00 AM – 8:00 PM daily. To book a treatment, contact the front desk or submit a request " +
                          "through 'My Requests'. We recommend booking in advance as slots fill up quickly."
            ),

            new(
                phrases: ["laundry service", "wash my clothes", "dry cleaning", "laundry"],
                keywords: ["laundry", "washing", "dry clean", "clothes"],
                response: "We offer a laundry and dry-cleaning service for guests. To use it:\n" +
                          "1. Place items in the laundry bag provided in your room\n" +
                          "2. Submit a request through 'My Requests' or call the front desk\n\n" +
                          "Standard turnaround is 24 hours. Express same-day service is available for an additional charge."
            ),

            new(
                phrases: ["parking available", "car park", "where to park", "park my car", "parking fee",
                          "is parking free", "how much is parking"],
                keywords: ["parking", "car", "garage", "vehicle", "park"],
                response: "Complimentary parking is available for all hotel guests in our car park behind the hotel. " +
                          "Please register your vehicle at the front desk on check-in so we can issue you a parking pass. " +
                          "The car park is monitored 24/7 by CCTV."
            ),

            // ── Transport ────────────────────────────────────────────────────────
            new(
                phrases: ["taxi", "cab", "uber", "how to get here", "directions", "transport to hotel",
                          "shuttle service", "airport transfer", "get to hotel", "nearest airport"],
                keywords: ["taxi", "transport", "directions", "airport", "shuttle"],
                response: "Getting to WebHotel:\n" +
                          "• **By taxi/Uber**: Give the driver our address and they'll bring you straight here\n" +
                          "• **Airport transfer**: We can arrange a private airport transfer — contact the front desk at least 24 hours in advance\n" +
                          "• **Public transport**: Ask the front desk for the nearest bus/train stops and directions\n\n" +
                          "Contact reception if you need help arranging transport."
            ),

            // ── Requests & My Requests ───────────────────────────────────────────
            new(
                phrases: ["what is my requests", "what are requests", "how to submit request", "how do requests work",
                          "what can i request", "submit a request"],
                keywords: ["requests", "request"],
                response: "'My Requests' is your main way to communicate your needs to hotel staff. You can submit:\n" +
                          "• New Booking requests\n" +
                          "• Extend Stay\n" +
                          "• Change Room\n" +
                          "• Early Checkout / Cancellation\n" +
                          "• Order Food / Room Service\n\n" +
                          "Click 'My Requests' in the navigation menu, then 'New Request'. Staff will review and respond — " +
                          "you'll see the status update to Approved or Rejected."
            ),

            new(
                phrases: ["request not approved", "request rejected", "why was my request rejected",
                          "request pending", "request taking too long", "how long does request take"],
                keywords: [],
                response: "Request processing times vary:\n" +
                          "• Most requests are reviewed within a few hours during staff hours\n" +
                          "• For urgent matters, please call the front desk directly\n\n" +
                          "If your request was rejected, the staff member may have added a note explaining why. " +
                          "Check 'My Requests' for any messages. You're welcome to submit a new request or speak to the front desk for clarification."
            ),

            // ── General Hotel Info ───────────────────────────────────────────────
            new(
                phrases: ["contact reception", "front desk number", "phone number", "how to contact staff",
                          "speak to someone", "talk to a person", "reach the hotel"],
                keywords: ["contact", "phone", "reception", "desk", "call"],
                response: "The front desk is staffed 24/7 and ready to help.\n\n" +
                          "• For urgent matters: call the front desk directly (number posted in your room)\n" +
                          "• For non-urgent matters: submit a request through 'My Requests'\n" +
                          "• For live chat: click 'Talk to Staff' here in this chat window\n\n" +
                          "We're always here to help!"
            ),

            new(
                phrases: ["noise complaint", "noisy room", "neighbours are loud", "too much noise", "disturbing noise"],
                keywords: ["noise", "noisy", "loud", "disturb"],
                response: "We're sorry to hear you're being disturbed. Please contact the front desk immediately by phone " +
                          "so staff can address the situation right away. You can also submit a request through 'My Requests' " +
                          "but calling is faster for noise complaints."
            ),

            new(
                phrases: ["housekeeping", "clean my room", "room cleaning", "when is housekeeping",
                          "change towels", "change bedding", "fresh towels"],
                keywords: ["housekeeping", "cleaning", "towels", "bedding", "sheets"],
                response: "Housekeeping visits your room daily between 9:00 AM and 1:00 PM. If you'd prefer a specific time " +
                          "or need fresh towels/bedding between visits, just:\n" +
                          "• Submit a request through 'My Requests', or\n" +
                          "• Call the front desk and we'll send someone up\n\n" +
                          "If you don't want your room disturbed, hang the 'Do Not Disturb' sign on your door."
            ),

            new(
                phrases: ["accessibility", "wheelchair", "disabled access", "disability", "accessible room",
                          "mobility", "elevator", "lift"],
                keywords: ["wheelchair", "accessible", "disabled", "mobility", "lift", "elevator"],
                response: "WebHotel is committed to accessibility:\n" +
                          "• Wheelchair-accessible rooms are available — request one when booking\n" +
                          "• The hotel has lifts/elevators on all floors\n" +
                          "• Accessible bathrooms with grab rails are available\n\n" +
                          "Please let us know your specific needs when booking so we can prepare the right room and assistance for you."
            ),

            new(
                phrases: ["lost and found", "lost something", "i lost my", "found something", "left something behind"],
                keywords: ["lost", "found", "left behind", "missing"],
                response: "If you've lost something or found an item, please contact the front desk right away. " +
                          "We keep all found items in our lost & found for 30 days. If you've already checked out, " +
                          "call us and we'll check if your item has been handed in and arrange to return it to you."
            ),

            new(
                phrases: ["how much does it cost", "room price", "room rate", "nightly rate",
                          "how much is a room", "price per night", "how much to stay"],
                keywords: ["price", "cost", "rate", "cheap", "expensive", "affordable"],
                response: "Our nightly rates:\n" +
                          "• **Twin Room**: from $100/night\n" +
                          "• **Deluxe Room**: from $150/night\n" +
                          "• **Suite**: from $300/night\n\n" +
                          "Prices may vary by season and availability. Use the search on the Home page to see exact pricing for your dates."
            ),

            // ── Greetings & General Help ─────────────────────────────────────────
            new(
                phrases: ["hello", "hi there", "good morning", "good evening", "good afternoon", "how are you"],
                keywords: ["hello", "hi", "hey", "hiya", "greetings"],
                response: "Hi there! Welcome to WebHotel. I'm here to help you with anything you need.\n\n" +
                          "I can assist with:\n" +
                          "• Bookings & reservations\n" +
                          "• Payments, deposits & refunds\n" +
                          "• Check-in & check-out times\n" +
                          "• Room changes, upgrades & extensions\n" +
                          "• Food orders & room service\n" +
                          "• WiFi, parking, pool, gym & spa\n" +
                          "• Housekeeping & maintenance\n" +
                          "• Account & password help\n\n" +
                          "What can I help you with today?"
            ),

            new(
                phrases: ["thank you", "thanks", "thank you so much", "cheers", "that helped"],
                keywords: ["thank", "thanks", "cheers"],
                response: "You're welcome! Is there anything else I can help you with? " +
                          "If you need to speak to a staff member directly, just click 'Talk to Staff' below."
            ),

            new(
                phrases: ["what can you help with", "what can you do", "what do you know", "help me",
                          "i need help", "i need assistance"],
                keywords: ["help", "assist", "support"],
                response: "I can help you with a wide range of hotel-related questions:\n\n" +
                          "**Bookings**: how to book, view, change, extend or cancel your stay\n" +
                          "**Payments**: how to pay, view receipts, deposits, refunds\n" +
                          "**Check-in/out**: times, early/late options, what to bring\n" +
                          "**Rooms**: room types, amenities, capacity, accessibility\n" +
                          "**Food**: room service, restaurant hours, dietary needs\n" +
                          "**Facilities**: WiFi, pool, gym, spa, parking, laundry\n" +
                          "**Issues**: maintenance, noise complaints, lost & found\n" +
                          "**Account**: password reset, profile updates\n\n" +
                          "Just ask away! If I can't answer, I'll connect you with a staff member."
            ),
        ];

        public string GetResponse(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                return "Please type a question and I'll do my best to help!";

            var lower = userMessage.ToLower().Trim();

            var bestMatch = FaqEntries
                .Select(faq =>
                {
                    // Phrases score by word count — longer = more specific = higher priority
                    int phraseScore = faq.phrases
                        .Where(p => lower.Contains(p))
                        .Select(p => p.Split(' ').Length)
                        .DefaultIfEmpty(0).Max();

                    // Keywords only count if no phrase matched
                    int keywordScore = phraseScore == 0
                        ? faq.keywords.Count(kw => lower.Contains(kw))
                        : 0;

                    return new { faq.response, Score = phraseScore * 10 + keywordScore };
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            if (bestMatch != null)
                return bestMatch.response;

            return "I'm not sure I understood that — sorry!\n\n" +
                   "I can help with bookings, payments, check-in/out, room changes, food orders, " +
                   "WiFi, parking, pool, gym, housekeeping, maintenance, or account/password issues.\n\n" +
                   "Try rephrasing your question, or click **'Talk to Staff'** below to chat with a real person right now.";
        }

        private record FaqEntry(string[] phrases, string[] keywords, string response);
    }
}
