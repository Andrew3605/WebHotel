# WebHotel

WebHotel is an ASP.NET Core MVC hotel booking system built with role-based access for customers and admins. It includes booking workflows, request handling, product management, account administration, a REST API with Swagger documentation, Docker support, structured logging, an admin dashboard with analytics, and email notifications.

## Highlights

- Customer registration and login with ASP.NET Core Identity
- Room browsing and booking management
- Customer request submission and admin review workflow
- Booking statement workflow with deposits, extra charges, refunds, and payment history
- Product and customer administration
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
