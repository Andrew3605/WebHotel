# WebHotel

WebHotel is an ASP.NET Core MVC hotel booking project with role-based access for admins and customers.

## Features

- Customer registration and login with ASP.NET Core Identity
- Room browsing and booking management
- Customer request management
- Product and customer administration
- Admin-only dashboards and account management tools

## Tech Stack

- ASP.NET Core MVC
- C#
- Entity Framework Core
- SQL Server
- Razor Views
- Bootstrap
- ASP.NET Core Identity

## Run Locally

1. Clone the repository.
2. Open `WebHotel.sln` in Visual Studio, or run the project from the `WebHotel` folder with `dotnet run`.
3. Check the connection string in `WebHotel/appsettings.json`.
4. Run the app. On startup, Entity Framework migrations are applied automatically.
5. For local Development runs, a demo admin account is seeded from `WebHotel/appsettings.Development.json`.

## Demo Admin Login

For local Development runs, the app seeds an admin account automatically on startup:

- Email: `admin@hotel.local`
- Password: `Admin!1234`

You can use that account to test the admin features, including customer management, bookings, requests, products, and password resets for customer accounts.

## Demo Notes

- If the admin account already exists in your local database, the seed step will not overwrite it.
- If login fails with the demo account, delete the existing local database or update the admin user manually.
- The app is configured for showcase/demo use in Development. For production deployment, move admin credentials to environment-specific secrets.

## Security Improvements

- Login attempts now use account lockout after repeated failures.
- Password rules are stronger for newly created accounts.
- Registration, login, and customer request submission are rate-limited.
- Customer request spam is reduced with pending-request caps and duplicate-request checks.
- Request note length is limited in the UI and controller validation.
