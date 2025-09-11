# Postmate API

A .NET 8 Web API backend for managing LinkedIn posts with AI integration, WhatsApp webhooks, and automated scheduling.

## Features

- **Authentication**: JWT-based authentication with hardcoded credentials
- **Posts Management**: CRUD operations for LinkedIn posts
- **AI Integration**: OpenAI API integration for generating post drafts
- **WhatsApp Integration**: Webhook endpoint for WhatsApp message handling
- **Scheduling**: Hangfire-based background job scheduling
- **Database**: Entity Framework Core with SQL Server
- **API Documentation**: Swagger UI for testing and documentation

## Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB for development)
- OpenAI API Key
- Visual Studio 2022 or VS Code (optional)

## Setup Instructions

### 1. Clone and Navigate
```bash
git clone <repository-url>
cd PostmateAPI
```

### 2. Install Dependencies
```bash
dotnet restore
```

### 3. Configure Database
Update the connection string in `appsettings.json` or `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=PostmateDB;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

### 4. Create Database
```bash
dotnet ef database update
```

### 5. Configure OpenAI API Key
Update `appsettings.json` with your OpenAI API key:
```json
{
  "OpenAI": {
    "ApiKey": "your-actual-openai-api-key-here"
  }
}
```

### 6. Run the Application
```bash
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `http://localhost:5000` (root URL)

## API Endpoints

### Authentication
- `POST /api/auth/login` - Login with credentials

**Default Credentials:**
- Username: `admin`, Password: `password123`
- Username: `user`, Password: `userpass`

### Posts
- `GET /api/posts` - Get all posts (requires authentication)
- `POST /api/posts` - Create a new post (requires authentication)
- `POST /api/posts/{id}/approve` - Approve a post (requires authentication)
- `POST /api/posts/{id}/reject` - Reject a post (requires authentication)

### WhatsApp Webhook
- `POST /api/webhook/whatsapp` - Handle WhatsApp messages (no authentication required)

**WhatsApp Commands:**
- `New Post: [topic]` - Create a new post with the specified topic
- `YES` - Approve the latest pending post
- `NO` - Reject the latest pending post and regenerate draft

## Database Schema

### Posts Table
- `Id` (int, Primary Key)
- `Topic` (string, Required, Max 500 chars)
- `Draft` (string, Nullable)
- `Status` (string, Required, Max 50 chars) - Values: "Pending", "Approved", "Posted"
- `ScheduledAt` (datetime, Nullable)
- `CreatedAt` (datetime, Default: UTC Now)

## Background Jobs

The application uses Hangfire to run background jobs:
- **Post Scheduler**: Checks every 5 minutes for approved posts that are ready to be published
- **Hangfire Dashboard**: Available at `/hangfire` (development only)

## Deployment

### Render.com Deployment

1. **Create a new Web Service** on Render.com
2. **Connect your GitHub repository**
3. **Configure the following settings:**
   - Build Command: `dotnet publish -c Release -o ./publish`
   - Start Command: `dotnet ./publish/PostmateAPI.dll`
   - Environment: `Production`

4. **Add Environment Variables:**
   - `ASPNETCORE_ENVIRONMENT`: `Production`
   - `ASPNETCORE_URLS`: `http://0.0.0.0:$PORT`
   - `OpenAI__ApiKey`: Your OpenAI API key
   - `Jwt__Key`: A secure JWT secret key (32+ characters)
   - `Jwt__Issuer`: `PostmateAPI`
   - `Jwt__Audience`: `PostmateAPI`

5. **Create a PostgreSQL Database** on Render.com and add the connection string as:
   - `ConnectionStrings__DefaultConnection`: Your PostgreSQL connection string

6. **Deploy** and run database migrations:
   ```bash
   dotnet ef database update --connection "your-postgresql-connection-string"
   ```

### Docker Deployment

Build and run with Docker:
```bash
docker build -t postmate-api .
docker run -p 5000:80 postmate-api
```

## Configuration

### JWT Settings
```json
{
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "PostmateAPI",
    "Audience": "PostmateAPI"
  }
}
```

### OpenAI Settings
```json
{
  "OpenAI": {
    "ApiKey": "your-openai-api-key-here"
  }
}
```

## Testing the API

1. **Start the application**
2. **Open Swagger UI** at the root URL
3. **Login** using the `/api/auth/login` endpoint
4. **Copy the JWT token** from the response
5. **Click "Authorize"** in Swagger UI and enter: `Bearer <your-jwt-token>`
6. **Test the endpoints** using the Swagger interface

## WhatsApp Integration

To test WhatsApp integration:
1. **Send a POST request** to `/api/webhook/whatsapp` with:
   ```json
   {
     "message": "New Post: Artificial Intelligence in Healthcare",
     "from": "+1234567890",
     "to": "+0987654321"
   }
   ```

2. **Approve the post** by sending:
   ```json
   {
     "message": "YES"
   }
   ```

## Troubleshooting

### Common Issues

1. **Database Connection Issues**
   - Ensure SQL Server LocalDB is installed
   - Check connection string format
   - Run `dotnet ef database update`

2. **OpenAI API Issues**
   - Verify API key is correct
   - Check API key has sufficient credits
   - Review OpenAI service logs

3. **JWT Authentication Issues**
   - Ensure JWT key is at least 32 characters
   - Check token expiration time
   - Verify Authorization header format: `Bearer <token>`

4. **Hangfire Issues**
   - Check database connection for Hangfire tables
   - Verify Hangfire dashboard is accessible at `/hangfire`
   - Review background job logs

## Development

### Project Structure
```
PostmateAPI/
├── Controllers/          # API Controllers
├── Data/                # Database Context
├── DTOs/                # Data Transfer Objects
├── Models/              # Entity Models
├── Services/            # Business Logic Services
├── Migrations/          # EF Core Migrations
└── Program.cs           # Application Entry Point
```

### Adding New Features
1. Create models in `Models/` folder
2. Add DTOs in `DTOs/` folder
3. Implement services in `Services/` folder
4. Create controllers in `Controllers/` folder
5. Add database migrations as needed

## License

This project is licensed under the MIT License.
