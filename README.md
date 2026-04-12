# WebHotel

WebHotel is an ASP.NET Core MVC hotel booking system built with role-based access for customers and admins. It includes booking workflows, request handling, product management, and account administration in a single full-stack web application.

## Highlights

- Customer registration and login with ASP.NET Core Identity
- Room browsing and booking management
- Customer request submission and admin review workflow
- Booking statement workflow with deposits, extra charges, refunds, and payment history
- Product and customer administration
- Role-based navigation and protected admin features

## Tech Stack

- ASP.NET Core MVC
- C#
- Entity Framework Core
- xUnit
- GitHub Actions
- SQL Server
- Razor Views
- Bootstrap
- ASP.NET Core Identity

## Getting Started

1. Clone the repository.
2. Open `WebHotel.sln` in Visual Studio, or run the app from the `WebHotel` folder with `dotnet run`.
3. Review the connection string in `WebHotel/appsettings.json`.
4. Start the application. Entity Framework migrations are applied automatically at startup.
5. In Development, a demo admin account is seeded from `WebHotel/appsettings.Development.json`.

## Demo Access

For local Development runs:

- Admin email: `admin@hotel.local`
- Admin password: `Admin!1234`

This account can be used to explore admin features such as booking management, customer request review, product management, and customer account administration.

## Notes

- If the admin account already exists in the local database, startup will not overwrite it.
- If the seeded admin login does not work, remove the local database and run the project again, or update the existing admin user manually.
- The seeded admin account is intended for local showcase and testing in Development only.

## Privacy Note

- This project is a demonstration application and stores standard demo account and booking information such as customer name, email, phone number, booking details, and customer requests.
- Passwords are managed through ASP.NET Core Identity rather than stored in plain text.
- The payment flow is a demo workflow. Full card details such as complete card number and CVV are not persisted to the database.

## Quality

- Automated tests cover booking payment calculations and core billing rules.
- GitHub Actions runs restore, build, and test checks on pushes and pull requests to `main`.

## Security

- Failed login attempts contribute to account lockout.
- Password requirements are stronger for newly created accounts.
- Registration, login, and customer request submission are rate-limited.
- Customer request spam is reduced with pending-request caps and duplicate-request checks.
- Request notes are limited in the UI and validated server-side.
