# Assignment Submission Project

This is a scalable, production-grade Assignment & Submission Management System designed to handle up to 100,000 users.

## Architecture

The application uses the following architecture:

```text
Next.js (Frontend)
      ↓
ASP.NET Core Web API (Backend)
      ↓
Npgsql / EF Core
      ↓
Neon PostgreSQL (Cloud Database)
```

**Note:** Redis caching and other scalability components (like background processing with Hangfire) will be implemented in a later phase.

## Prerequisites

- Node.js (v18+)
- .NET 10.0 SDK
- A [Neon](https://neon.tech/) Serverless Postgres database

## Setup Instructions

### 1. Database Configuration

We use Neon PostgreSQL as our cloud-hosted database. **Never commit real credentials or `.env` files to this repository.**

1. Obtain a connection string from your Neon dashboard.
2. In the project root, copy the `.env.example` file to create a `.env` file (or configure environment variables locally).
3. If using ASP.NET Core User Secrets for local development (recommended), run the following in the `backend/AssignmentSystem.Api` folder:
   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_NEON_CONNECTION_STRING_HERE"
   ```

### 2. Backend Setup (ASP.NET Core)

Navigate to the `backend` directory:
```bash
cd backend
```

Restore dependencies:
```bash
dotnet restore
```

Apply Entity Framework Core migrations to create the database schema in Neon:
```bash
cd AssignmentSystem.Api
dotnet ef database update
```

Run the backend API:
```bash
dotnet run
```

### 3. Frontend Setup (Next.js)

Navigate to the `frontend` directory:
```bash
cd frontend
```

Install dependencies:
```bash
npm install
```

Run the development server:
```bash
npm run dev
```

## Security Requirements

- **Do NOT** commit database passwords or full connection strings.
- **Do NOT** commit `.env` or `.env.*` files.
- **Do NOT** commit API keys or access tokens.
- Only safe examples/placeholders should be committed to GitHub.
