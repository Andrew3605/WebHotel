# WebHotel

WebHotel is an ASP.NET Core MVC hotel booking system built with three-tier role-based access (Admin, Staff, Customer). It includes booking workflows, request handling, room service ordering, product management, account administration, a REST API with Swagger documentation, Docker support, structured logging, an admin dashboard with analytics, email notifications, an AI-powered customer chat helper with live staff escalation, a staff help guide bot, admin-managed staff accounts, and a comprehensive audit trail.

## Highlights

- Customer registration and login with ASP.NET Core Identity
- Three-tier role hierarchy: Admin > Staff > Customer with granular permissions
- Room browsing and booking management
- Customer request submission and admin/staff review workflow
- Booking statement workflow with deposits, extra charges, refunds, and payment history
- Room service ordering: staff can charge products from the catalog directly to bookings
- Product and customer administration
- AI-powered customer chat helper with live staff escalation via SignalR
- Chat history restored on login — customers see previous messages after logging out and back in
- Staff help guide bot — floating widget for staff/admin explaining how the system works
- Admin-managed staff accounts — create and delete Staff logins from the UI
- Cleaned-up admin navbar with grouped Manage dropdown and visible Logout button
- Audit logging for all admin/staff actions (create, edit, delete, approve, reject)
- Optimistic concurrency control on bookings (RowVersion timestamp)
- Role-based navigation and protected admin features
- REST API with Swagger/OpenAPI documentation
- Docker and Docker Compose for containerized deployment
- Structured logging with Serilog (console + rolling file)
- Admin dashboard with occupancy, revenue, and booking charts (Chart.js)
- Email notifications for booking confirmations, payment receipts, and request status updates
- Full customer request workflow: NewBooking, ExtendStay, ChangeRoom, EarlyCheckout, OrderFood
- Paginated admin views with query string search persistence
- Automated test suite with 39 tests (unit + API controller tests)

## Tech Stack

- ASP.NET Core MVC (.NET 8)
- C#
- Entity Framework Core
- SQL Server
- ASP.NET Core Identity
- Razor Views + Bootstrap
- REST API + Swagger/OpenAPI (Swashbuckle)
- Serilog (structured logging)
- SignalR (real-time live chat)
- Chart.js (admin dashboard charts)
- SMTP email notifications
- Docker + Docker Compose
- xUnit (unit + integration tests)
- GitHub Actions (CI)

## Getting Started

### Option 1: Local Development

1. Clone the repository.
2. Open `WebHotel.sln` in Visual Studio, or run the app from the `WebHotel` folder with `dotnet run`.
3. Review the connection string in `WebHotel/appsettings.json`.
4. Start the application. Entity Framework migrations are applied automatically at startup.
5. In Development, a demo admin account is seeded from `WebHotel/appsettings.Development.json`.

### Option 2: Docker

```bash
docker-compose up --build
```

This starts:
- **SQL Server** container on port 1433
- **WebHotel** app on port 8080

The app auto-applies migrations and seeds the demo admin account.

## Roles and Permissions

The system uses a three-tier role hierarchy:

| Feature | Admin | Staff | Customer |
|---------|:-----:|:-----:|:--------:|
| Dashboard & analytics | Yes | - | - |
| Manage rooms & products | Yes | - | - |
| View audit log | Yes | - | - |
| Delete bookings/customers | Yes | - | - |
| Create/edit bookings | Yes | Yes | - |
| Manage customer requests | Yes | Yes | - |
| Room service (charge products) | Yes | Yes | - |
| View customers list | Yes | Yes | - |
| Front desk payments/charges | Yes | Yes | - |
| Live chat dashboard (answer customers) | Yes | Yes | - |
| Browse rooms & book online | - | - | Yes |
| View own bookings & pay online | - | - | Yes |
| Submit requests | - | - | Yes |
| AI chat helper (customer bot + live staff) | - | - | Yes |
| Staff help guide bot | Yes | Yes | - |
| Create / delete staff accounts | Yes | - | - |

## Demo Access

For local Development runs:

- Admin email: `admin@hotel.local`
- Admin password: `Admin!1234`

This account can be used to explore admin features such as booking management, customer request review, product management, and customer account administration.

## REST API

The application exposes a full REST API alongside the MVC views:

| Endpoint | Auth | Description |
|----------|------|-------------|
| `GET /api/RoomsApi` | Public | List all rooms |
| `GET /api/RoomsApi/availability` | Public | Search available rooms by dates and guests |
| `GET /api/RoomsApi/{id}` | Public | Get room details |
| `POST /api/RoomsApi` | Admin | Create a room |
| `PUT /api/RoomsApi/{id}` | Admin | Update a room |
| `DELETE /api/RoomsApi/{id}` | Admin | Delete a room |
| `GET /api/BookingsApi` | Admin | List all bookings |
| `GET /api/BookingsApi/{id}` | Admin | Get booking details |
| `GET /api/BookingsApi/{id}/statement` | Admin | Get booking payment statement |
| `POST /api/BookingsApi` | Admin | Create a booking |
| `PUT /api/BookingsApi/{id}` | Admin | Update a booking |
| `DELETE /api/BookingsApi/{id}` | Admin | Delete a booking |
| `GET /api/CustomersApi` | Admin | List all customers |
| `GET /api/CustomersApi/{id}` | Admin | Get customer details |
| `POST /api/CustomersApi` | Admin | Create a customer |
| `PUT /api/CustomersApi/{id}` | Admin | Update a customer |
| `DELETE /api/CustomersApi/{id}` | Admin | Delete a customer |
| `GET /api/PaymentsApi/booking/{id}` | Admin | Get payment entries for a booking |
| `GET /api/PaymentsApi/booking/{id}/summary` | Admin | Get financial summary for a booking |

### Swagger UI

Interactive API documentation is available at `/swagger` when the app is running.

## Admin Dashboard

Admins have access to a dashboard at `/Home/Dashboard` showing:

- **Summary cards**: total bookings, revenue received, outstanding balance, pending requests
- **Occupancy rate**: rooms occupied today vs total rooms with progress bar
- **Bookings chart**: bar chart of bookings per month (last 6 months) via Chart.js
- **Revenue chart**: doughnut chart of revenue breakdown by room type
- **Recent bookings table**: last 5 bookings with payment status and quick links

## Email Notifications

The system sends HTML email notifications at key workflow points:

- **Booking confirmation** when a customer request is approved and a booking is created
- **Payment receipt** when a customer makes an online payment
- **Request status update** when an admin approves or rejects a customer request

Email is configured via SMTP settings in `appsettings.json`. Delivery failures are logged but do not block the user workflow.

## Room Service

Staff and admins can charge products from the hotel catalog directly to a guest's booking:

1. Navigate to a booking's **Statement** page
2. Click **Room Service** to browse the product catalog
3. Filter by category, set quantity, and add an optional note
4. Each charge creates a `PaymentEntry` of type `Charge` linked to the booking
5. Charges appear on the booking statement and update the balance due automatically

## AI Customer Chat + Live Staff Escalation

Logged-in customers have access to a floating chat widget with two modes:

### Bot Mode
- Answers 30+ hotel topics: bookings, payments, check-in/out, room types, amenities, WiFi, parking, pool, gym, spa, laundry, food/restaurant, accessibility, noise complaints, lost & found, account/password help, and more
- Phrase-priority keyword matching engine — longer phrases score higher so "reset password" never accidentally matches WiFi
- Pluggable behind an `IChatBotService` interface — swap to any AI provider (Groq, OpenAI, Gemini, etc.) without touching the hub or widget
- Chat history is restored on login — customers see their full previous conversation after logging out and back in

### Live Staff Chat (SignalR)
- After the bot replies twice, a **"Talk to Staff"** button appears
- Clicking it places the customer in a queue and notifies staff in real time
- Staff see waiting chats on the **Live Chat Dashboard** (`/StaffChat`) with audio notifications
- Staff can pick up, reply in real time, and close sessions
- All messages (bot + staff + customer) are persisted to the database
- Chat history is viewable at `/StaffChat/History`
- Built with **ASP.NET Core SignalR** for real-time WebSocket communication

## Staff Help Guide

Staff and admin users have a floating **yellow `?` button** in the bottom-right corner of every page. Clicking it opens an interactive help bot that explains how the WebHotel system works — no AI, no external calls, no charges.

Topics covered:
- How to approve or reject customer requests
- How to create, edit, and delete bookings
- How the live chat system works and how to pick up a waiting customer
- How to view and search customers
- How to reset a customer password
- How to add, edit, and delete rooms and products (Admin)
- How to create and delete staff accounts (Admin)
- How to use the dashboard and audit log (Admin)
- How payments, deposits, and email notifications work

The widget is invisible to customers — only Staff and Admin roles see it.

## Staff Account Management

Admins can create and delete Staff accounts directly from **Manage → Staff Accounts** without needing database access or configuration files:

1. Go to **Manage → Staff Accounts**
2. Click **New Staff Account**
3. Enter the staff member's email and a password
4. The account is created immediately with the Staff role

The page also shows a full permissions breakdown — what Staff can and cannot do — for easy reference.

## Audit Logging

All admin and staff actions are recorded in a searchable audit log:

- Tracks action type, entity, user, role, and timestamp
- Covers booking CRUD, customer request approvals/rejections, payment entries, and room service charges
- Searchable and paginated at `/Home/AuditLog` (Admin only)

## Concurrency Control

Bookings use optimistic concurrency with a `[Timestamp]` RowVersion column:

- If two users edit the same booking simultaneously, the second save is rejected with a user-friendly message
- The user is prompted to reload and retry, preventing silent data overwrites

## Customer Request Workflow

All request types are now fully handled on admin approval:

| Request Type | What Happens on Approval |
|-------------|--------------------------|
| NewBooking | Creates a new booking with conflict detection |
| ExtendStay | Extends the checkout date and recalculates the price |
| ChangeRoom | Moves the booking to a different room and recalculates |
| EarlyCheckout | Shortens the stay to the requested date and recalculates |
| OrderFood | Status change only (no automated booking action) |

Conflicting requests (room overlap) are automatically rejected.

## Logging

Structured logging is powered by Serilog with two sinks:

- **Console** for development and Docker container logs
- **Rolling file** at `Logs/webhotel-YYYYMMDD.log` (30-day retention)

HTTP request logging is enabled via `UseSerilogRequestLogging()`, providing method, path, status code, and response time for every request.

## Quality

- **39 automated tests** covering:
  - Booking payment calculator (unit tests)
  - Rooms API: CRUD, search, availability with conflict detection
  - Bookings API: CRUD, date conflict validation, non-existent entity handling
  - Customers API: CRUD and search
  - Payments API: payment entries retrieval and financial summary calculations
- **GitHub Actions** runs restore, build, and test checks on pushes and pull requests to `main`.

## Security

- Failed login attempts contribute to account lockout (5 attempts, 15-minute lockout).
- Password requirements are stronger for newly created accounts (10+ chars, mixed case, digit, special char).
- Registration, login, and customer request submission are rate-limited.
- Customer request spam is reduced with pending-request caps and duplicate-request checks.
- Request notes are limited in the UI and validated server-side.
- API endpoints enforce role-based authorization.

## Notes

- If the admin account already exists in the local database, startup will not overwrite it.
- If the seeded admin login does not work, remove the local database and run the project again, or update the existing admin user manually.
- The seeded admin account is intended for local showcase and testing in Development only.

## Privacy Note

- This project is a demonstration application and stores standard demo account and booking information such as customer name, email, phone number, booking details, and customer requests.
- Passwords are managed through ASP.NET Core Identity rather than stored in plain text.
- The payment flow is a demo workflow. Full card details such as complete card number and CVV are not persisted to the database.
