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

1. Open the solution in Visual Studio or run the project from the `WebHotel` folder.
2. Update the `DefaultConnection` string in `WebHotel/appsettings.json` if needed.
3. Run the app. On startup, Entity Framework migrations are applied automatically.

## Demo Admin Login

The app seeds an admin account automatically on startup:

- Email: `admin@hotel.local`
- Password: `Admin!123`

You can use that account to test the admin features, including customer management, bookings, requests, products, and password resets for customer accounts.
