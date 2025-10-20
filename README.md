# Demo Project - Logistics Tracking System

A comprehensive logistics tracking system built with ASP.NET Core 8.0, Entity Framework Core, and PostgreSQL.

## Features

- User authentication and authorization (Admin, Driver, Customer roles)
- Shipment management and tracking with address-based locations
- Smart driver assignment system with region-based matching
- Real-time notifications via SendGrid
- Audit logging
- Comprehensive reporting system
- RESTful API with detailed documentation

## Prerequisites

- .NET 8.0 SDK
- PostgreSQL database
- SendGrid account (for email notifications)

## Setup Instructions

### 1. Clone the Repository
```bash
git clone https://github.com/YOUR_USERNAME/demoProject.git
cd demoProject
```

### 2. Database Configuration
1. Install PostgreSQL if not already installed
2. Create a new database
3. Copy `appsettings.template.json` to `appsettings.json`
4. Update the connection string in `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=YourDatabase;Username=YourUsername;Password=YourPassword"
   }
   ```

### 3. JWT Configuration
Update the JWT settings in `appsettings.json`:
```json
"Jwt": {
  "Key": "your-super-secret-jwt-key-that-is-at-least-32-characters-long",
  "Issuer": "LogisticTracker",
  "Audience": "LogisticTrackerUsers"
}
```

### 4. SendGrid Configuration (Optional)
1. Create a SendGrid account and get your API key
2. Update the SendGrid settings in `appsettings.json`:
   ```json
   "SendGrid": {
     "ApiKey": "YOUR_SENDGRID_API_KEY",
     "FromEmail": "your-verified-email@example.com"
   }
   ```

### 5. Run the Application
```bash
cd demoProject
dotnet restore
dotnet ef database update
dotnet run
```

The application will be available at `https://localhost:7048` or `http://localhost:5048`.

## API Documentation

See the following files for detailed API documentation:
- `API_EXAMPLES.md` - Complete API examples and usage
- `FRONTEND_API_GUIDE.md` - Frontend integration guide
- `COOKIE_AUTH_GUIDE.md` - Cookie-based authentication guide

## Project Structure

- **Controllers/** - API controllers
- **Models/** - Entity models
- **DTOs/** - Data Transfer Objects
- **Services/** - Business logic services
- **Data/** - Database context and configurations
- **Middleware/** - Custom middleware components
- **Tests/** - Unit and integration tests

## Authentication

The system supports both JWT and cookie-based authentication:
- JWT tokens for API access
- Cookie authentication for web applications
- Automatic token refresh functionality

## Security Notes

?? **Important**: Never commit sensitive configuration files containing:
- Database passwords
- JWT secret keys
- API keys (SendGrid, etc.)

Always use environment variables or secure configuration providers in production.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is for demonstration purposes.