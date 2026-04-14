# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore dependencies (cached layer)
COPY WebHotel/WebHotel.csproj WebHotel/
COPY WebHotel.Tests/WebHotel.Tests.csproj WebHotel.Tests/
COPY WebHotel.sln .
RUN dotnet restore

# Copy everything and publish
COPY . .
RUN dotnet publish WebHotel/WebHotel.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WebHotel.dll"]
