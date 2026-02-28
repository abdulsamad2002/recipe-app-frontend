# Recipe Sharing Platform — Fullstack

A fullstack recipe sharing application built with **Angular 18** (frontend) and **ASP.NET Core 10** (backend).

## Project Structure

```
Recipe App Fullstack/
├── recipe-app/                  # Angular 18 frontend
│   └── src/app/
│       ├── home/                # Feed page (search, categories, pagination)
│       ├── pages/
│       │   ├── login/           # Login page
│       │   ├── register/        # Register page
│       │   ├── recipe-detail/   # Full recipe view + comments + ratings
│       │   └── recipe-form/     # Create / edit recipe
│       ├── services/            # AuthService, RecipeService, etc.
│       ├── interceptors/        # JWT auth interceptor
│       └── pipes/               # ImageUrlPipe (backend URL resolver)
│
└── RecipeBackendHackathon/      # ASP.NET Core 10 Web API
    ├── Controllers/             # Auth, Recipes, Comments, Ratings, Categories
    ├── Services/                # Business logic layer
    ├── Repositories/            # ADO.NET search repository
    ├── DTOs/                    # Request / response data shapes
    ├── Models/                  # EF Core entity models
    ├── Data/                    # DbContext + EF migrations
    └── Middleware/              # Global exception handler
```

## Tech Stack

| Layer    | Technology                                               |
| -------- | -------------------------------------------------------- |
| Frontend | Angular 18, TypeScript, Angular Signals, RxJS            |
| Backend  | ASP.NET Core 10, Entity Framework Core, ADO.NET (search) |
| Database | SQL Server (LocalDB for dev)                             |
| Auth     | JWT Bearer tokens, BCrypt password hashing               |
| Styling  | Vanilla CSS, custom design system                        |

## Features

- 🔐 JWT authentication (register / login / logout)
- 🍽️ Recipe CRUD with image upload
- 🔍 Full-text search by keyword, ingredient, or category
- 📄 Pagination and multiple sort modes (newest, oldest, top-rated)
- ⭐ Star ratings with score distribution
- 💬 Paginated comments with edit & delete
- 📁 10 seeded categories (Vegetarian, Vegan, Dessert, etc.)

## Getting Started

### Prerequisites

- Node.js 20+ and npm
- .NET 10 SDK
- SQL Server LocalDB (ships with Visual Studio)

### 1 — Start the Backend

```bash
cd RecipeBackendHackathon
dotnet run
# API listens on http://localhost:5206
# Database is created & migrations applied automatically on first run
```

### 2 — Start the Frontend

```bash
cd recipe-app
npm install
npm start
# App opens at http://localhost:4200
```

### 3 — Open in Browser

Navigate to **http://localhost:4200** — register an account and start sharing recipes!

### Swagger UI (API docs)

Available at **http://localhost:5206/swagger** while the backend is running.

## Environment Variables

| File                                         | Variable                              | Default                     |
| -------------------------------------------- | ------------------------------------- | --------------------------- |
| `recipe-app/src/environments/environment.ts` | `apiBase`                             | `http://localhost:5206/api` |
| `recipe-app/src/environments/environment.ts` | `mediaBase`                           | `http://localhost:5206`     |
| `RecipeBackendHackathon/appsettings.json`    | `Jwt:Key`                             | Change in production!       |
| `RecipeBackendHackathon/appsettings.json`    | `ConnectionStrings:DefaultConnection` | LocalDB                     |
