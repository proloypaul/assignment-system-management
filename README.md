# 📚 Assignment Submission System

A full-stack, role-based **Assignment & Submission Management System** designed for educational institutions. It provides a centralized platform for A**Admin**, **Teacher**, and **Student** to manage courses, subjects, assignments, and student submissions efficiently, with PDF attachment support for secure and streamlined academic document management.

---

## 📖 Table of Contents

- [Project Overview](#-project-overview)
- [Main Features](#-main-features)
- [Technology Stack](#-technology-stack)
- [Project Structure](#-project-structure)
- [Prerequisites](#-prerequisites)
- [Setup Instructions](#-setup-instructions)
  - [1. Clone the Repository](#1-clone-the-repository)
  - [2. Backend Configuration](#2-backend-configuration)
  - [3. Frontend Configuration](#3-frontend-configuration)
- [Database Setup](#-database-setup)
- [Running the Application](#-running-the-application)
  - [Run the Backend](#run-the-backend)
  - [Run the Frontend](#run-the-frontend)
- [Default Seed Credentials](#-default-seed-credentials)
- [Running the Tests](#-running-the-tests)
- [API Overview](#-api-overview)
- [Assumptions](#-assumptions)
- [Known Limitations](#-known-limitations)

---

## 🌟 Project Overview

This system enables educational institutions to manage the full lifecycle of academic assignments:

- **Admins** configure the platform — managing users, courses, subjects, and assigning teachers to subjects.
- **Teachers** create, edit, publish assignments for their assigned subjects and grade student submissions with marks and written feedback.
- **Students** enroll in active courses, view published assignments, submit PDF answers, and track their grades.

Authentication is handled via **JWT access tokens** stored in `HttpOnly` cookies, with automatic silent refresh using rotating refresh tokens, ensuring sessions remain alive as long as the refresh token is valid.

---

## ✨ Main Features

### Admin Panel
- User management (view, role filter, delete users)
- Course management (create, edit, delete, view enrolled students, remove enrollments)
- Subject management (create, edit, delete, assign teacher — one teacher per subject)
- Assignment overview (view all assignments regardless of status, access any assignment's submissions)

### Teacher Portal
- View only assignments for their assigned subjects
- Create, edit, publish, and delete assignments (saved as Draft initially)
- View and grade student submissions with marks and feedback
- PDF attachment viewing from submission table

### Student Portal
- Browse all available courses and self-enroll (one active course at a time; enforced by backend)
- View published assignments for their enrolled courses
- Submit assignments with optional PDF attachment upload
- Track submission status and view received grades/feedback

### Platform-Wide
- Role-based access control (RBAC) on every route and API endpoint
- Responsive dashboard with collapsible sidebar (desktop) and slide-out drawer (mobile)
- Toast notifications (Sonner) for all user actions — no window alert popups
- Automatic silent JWT refresh on token expiry; graceful logout when refresh token expires
- Token expiry configurable per-environment via `appsettings.json`

---

## 🛠️ Technology Stack

### Backend
| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 10 Web API |
| ORM | Entity Framework Core 10 + Npgsql (PostgreSQL) |
| Auth | ASP.NET Identity + JWT Bearer (HttpOnly cookies) |
| Mediator | MediatR (CQRS pattern) |
| API Versioning | Asp.Versioning |
| Database | PostgreSQL (Neon serverless) |

### Frontend
| Layer | Technology |
|---|---|
| Framework | Next.js 16 (App Router, TypeScript) |
| State Management | Zustand (auth state) |
| Server State | TanStack React Query |
| Forms | React Hook Form + Zod validation |
| HTTP Client | Axios (with auto refresh interceptor) |
| Styling | Vanilla CSS + Tailwind CSS |
| Notifications | Sonner |
| Icons | Lucide React |

---

## 📁 Project Structure

```
assignment-submission-project/
├── .gitignore
├── README.md
│
├── backend/
│   ├── AssignmentSystem.slnx           # Solution file
│   ├── AssignmentSystem.Api/           # ASP.NET Core Web API (controllers, middleware, Program.cs)
│   │   ├── appsettings.json            # Shared / base configuration (committed)
│   │   ├── appsettings.Development.json  ← YOU CREATE THIS (see Setup)
│   │   ├── appsettings.Production.json   ← YOU CREATE THIS (see Setup)
│   │   ├── Controllers/V1/             # All API controllers
│   │   └── wwwroot/uploads/submissions/  # PDF file storage (auto-created on first upload)
│   ├── AssignmentSystem.Application/   # MediatR commands, interfaces, DTOs
│   ├── AssignmentSystem.Domain/        # Entities, enums, domain contracts
│   ├── AssignmentSystem.Infrastructure/ # DbContext, EF migrations, services, seed data
│   └── AssignmentSystem.Tests/         # Unit/integration test project
│
└── frontend/
    ├── .env.development                ← YOU CREATE THIS (see Setup)
    ├── .env.production                 ← YOU CREATE THIS for production
    ├── config.ts                       # Reads NEXT_PUBLIC_SERVERDOMEN_URL
    ├── src/
    │   ├── app/                        # Next.js App Router pages
    │   │   ├── admin/                  # Admin pages (users, courses, subjects, assignments)
    │   │   ├── teacher/                # Teacher pages (assignments, submissions)
    │   │   ├── student/                # Student pages (courses, assignments, submissions)
    │   │   ├── login/
    │   │   └── register/
    │   ├── components/                 # Shared UI components (DataTable, Modal, Button, etc.)
    │   ├── lib/                        # API client, types, query keys
    │   └── store/                      # Zustand auth store
    └── package.json
```

---

## ⚙️ Prerequisites

Make sure the following are installed before continuing:

| Tool | Minimum Version | Download |
|---|---|---|
| .NET SDK | 10.0 | https://dotnet.microsoft.com/download |
| Node.js | 18.x LTS | https://nodejs.org |
| PostgreSQL | 14+ (or use Neon.tech) | https://neon.tech |
| EF Core CLI | latest | `dotnet tool install -g dotnet-ef` |

---

## 🚀 Setup Instructions

### 1. Clone the Repository

```bash
git clone <your-repo-url>
cd assignment-submission-project
```

---

### 2. Backend Configuration

The backend uses ASP.NET's layered configuration system. Secrets and environment-specific values are **not committed** to Git. You must create the appropriate file before running.

#### For Local Development — create the file:

```
backend/AssignmentSystem.Api/appsettings.Development.json
```

**Paste the following content and fill in your own values:**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=YOUR_DB_HOST;Port=5432;Database=YOUR_DB_NAME;Username=YOUR_DB_USER;Password=YOUR_DB_PASSWORD;SSL Mode=Require;"
  },
  "Jwt__SecretKey": "YourSuperSecretKeyAtLeast32CharactersLong!@#",
  "Seed__AdminEmail": "******",
  "Seed__AdminPassword": "****",
  "Seed__TeacherEmail": "******",
  "Seed__TeacherPassword": "******",
  "Seed__StudentEmail": "******",
  "Seed__StudentPassword": "******",
  "Cookie__Secure": "false",
  "Cookie__SameSite": "Lax"
}
```

> **Note:** The base `appsettings.json` already contains these shared values (not secret):
> ```
> Jwt__Issuer, Jwt__Audience, Jwt__AccessTokenExpirationMinutes, Jwt__RefreshTokenExpirationDays, Cors__AllowedOrigins, Cookie__AccessTokenExpirationMinutes": "130", "Cookie__RefreshTokenExpirationDays
> ```
> You only need to override them in your environment-specific file if you want different values.

#### Key Configuration Reference

| Key | Description | Default (in appsettings.json) |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | *(empty — must be set)* |
| `Jwt__SecretKey` | JWT signing key (min 32 chars) | *(must be set in env file)* |
| `Jwt__AccessTokenExpirationMinutes` | Access token lifetime in minutes | `60` |
| `Jwt__RefreshTokenExpirationDays` | Refresh token lifetime in days | `7` |
| `Cors__AllowedOrigins` | Allowed frontend origin | `http://localhost:3000` |
| `Cookie__Secure` | Set `true` in production (HTTPS only) | `false` |
| `Seed__AdminEmail` | Seeded admin account email |
| `Seed__AdminPassword` | Seeded admin account password | 
| `Seed__TeacherEmail` | Seeded teacher account email | 
| `Seed__TeacherPassword` | Seeded teacher account password | 
| `Seed__StudentEmail` | Seeded student account email |
| `Seed__StudentPassword` | Seeded student account password | 

#### For Production

Create `backend/AssignmentSystem.Api/appsettings.Production.json` with the same structure, replacing values with your production database, a strong secret key, and setting `Cookie__Secure` to `true`.

---

### 3. Frontend Configuration

The frontend reads the backend API URL from an environment variable. These files are also **not committed** to Git.

#### For Local Development — create the file:

```
frontend/.env.development
```

**Paste the following:**

```env
NEXT_PUBLIC_SERVERDOMEN_URL=http://localhost:5221/api/v1
```

> Change the port (`5221`) if your backend runs on a different port.

#### For Production — create the file:

```
frontend/.env.production
```

```env
NEXT_PUBLIC_SERVERDOMEN_URL=https://your-production-api-domain.com/api/v1
```

---

## 🗄️ Database Setup

The project uses **Entity Framework Core** migrations. Once your `ConnectionStrings__DefaultConnection` is configured, run the following from the repository root:

```bash
cd backend

# Apply all EF migrations to create the database schema
dotnet ef database update \
  --project AssignmentSystem.Infrastructure \
  --startup-project AssignmentSystem.Api

# The application will automatically seed the database with the default
# Admin, Teacher, and Student accounts on first startup.
```

> **Using Neon.tech (recommended for cloud)?**
> 1. Create a free project at https://neon.tech
> 2. Copy the connection string (select **Node.js** format, then convert to ADO.NET format)  
>    Example: `Host=ep-xxx.us-east-2.aws.neon.tech;Port=5432;Database=neondb;Username=neondb_owner;Password=xxx;SSL Mode=Require;`
> 3. Paste it as `ConnectionStrings__DefaultConnection` in your `appsettings.Development.json`

---

## ▶️ Running the Application

### Run the Backend

```bash
cd backend/AssignmentSystem.Api

dotnet run
```

The API will be available at:
- **HTTP:** `http://localhost:5221`
- **Swagger UI:** `http://localhost:5221/swagger` *(development only)*

### Run the Frontend

```bash
cd frontend

# Install dependencies (first time only)
npm install

# Start the development server
npm run dev
```

The frontend will be available at: **`http://localhost:3000`**

> **Run both simultaneously** — open two terminal windows, one for each command.

---

## 🔐 Default Seed Credentials

On first startup the backend automatically creates these accounts:

| Role | Email | Password |
|---|---|---|
| **Admin** | `***` | `***` |
| **Teacher** | `***` | `***` |
| **Student** | `***` | `***` |

> These credentials are read from `appsettings.Development.json`. You can customize them to anything you prefer.

---

## 🧪 Running the Tests

The `AssignmentSystem.Tests` project contains the test suite.

```bash
cd backend

dotnet test
```

> The test project scaffold is in place. Unit tests for command handlers and integration tests for API endpoints can be added to `AssignmentSystem.Tests/`.

---

## 📡 API Overview

All API routes are versioned under `/api/v1/`. Key endpoint groups:

| Resource | Base Route | Auth |
|---|---|---|
| Authentication | `/auth` | Public (login/register) |
| Users | `/users` | Admin |
| Courses | `/courses` | Admin / Student (enroll) |
| Subjects | `/subjects` | Admin (write) / All (read) |
| Assignments | `/assignments` | Teacher/Admin (write) / Student (read) |
| Submissions | `/submissions` | Student (submit) / Teacher/Admin (grade/view) |

Swagger documentation is available at `http://localhost:5221/swagger` when running in development mode.

---

## 💡 Assumptions

1. **One teacher per subject** — When an Admin assigns a teacher to a subject, any previous teacher assignment is automatically replaced. A subject can only have one assigned teacher at a time.
2. **One active course per student** — A student cannot enroll in a new course while currently enrolled in a course whose end date has not yet passed.
3. **Course enrollment is time-gated** — Students can only self-enroll in a course if `now` falls within the course's `StartDate` and `EndDate` window.
4. **PDF-only file uploads** — Only `.pdf` files are accepted for assignment submissions. Files are stored in `wwwroot/uploads/submissions/` and served as static files.
5. **Teachers only see their subjects** — When a teacher creates an assignment, only subjects they are explicitly assigned to are available in the dropdown.
6. **Draft assignments are private** — Only Teachers (their own) and Admins (all) can see Draft assignments. Students only see Published ones.
7. **Token-based auth is cookie-only** — All tokens are stored in `HttpOnly` cookies. No tokens are stored in `localStorage`.
8. **Auto token refresh** — When an access token expires, the frontend silently calls `/auth/refresh-token` and retries the original request. If the refresh token is also expired, the user is logged out.

---


