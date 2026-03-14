# TaskFlow — Docker Setup Guide

## What is this?

This guide explains how to run the entire TaskFlow application using Docker.
Instead of installing .NET, Node.js, and SQL Server separately on your machine,
Docker packages everything into isolated containers and runs them together.

**You only need one tool: Docker Desktop.**

---

## How it works

```
Your Browser
     |
     | http://localhost:8090
     ▼
┌─────────────────────────────────────────────────┐
│              Docker Network (taskflow-net)      │
│                                                 │
│  ┌─────────────────┐      ┌──────────────────┐  │
│  │    Frontend     │      │     Backend      │  │
│  │  React + Nginx  │────▶│  ASP.NET Core 9  |  │
│  │   port 80       │ /api │    port 5110     │  │
│  └─────────────────┘      └────────┬─────────┘  │
│                                    │            │
│                            ┌───────▼──────────┐ │
│                            │    Database      │ │
│                            │  SQL Server 2022 │ │
│                            │    port 1433     │ │
│                            └──────────────────┘ │
└─────────────────────────────────────────────────┘
```

Three containers run together:

- **Frontend** — React app built with Vite, served by Nginx
- **Backend** — ASP.NET Core 9 REST API with JWT authentication
- **Database** — SQL Server 2022 (Developer edition, free)

The browser only ever talks to the Frontend on port 8080.
The Frontend proxies all `/api` calls to the Backend.
The Backend connects to the Database inside the Docker network.
No service is directly exposed to the internet except the Frontend.

---

## Prerequisites

Install **Docker Desktop** for your operating system:

- Windows / Mac: https://www.docker.com/products/docker-desktop
- Linux: https://docs.docker.com/engine/install/

After installing, verify it works:

```bash
docker --version
docker compose version
```

That is all you need. No .NET SDK, no Node.js, no SQL Server installation required.

---

## First-time setup

### 1. Clone the repository

```bash
git clone https://github.com/Kashmitha/dockerized-taskflow-project.git
cd dockerized-taskflow-project
```

### 2. Create your environment file

Copy the example and fill in your own values:

```bash
cp .env.example .env
```

Open `.env` and set:

```env
DB_PASSWORD=YourStrong@Password123
JWT_KEY=YourSuperSecretJwtKeyThatIsAtLeast32CharactersLong
```

Rules for these values:

- `DB_PASSWORD` must be at least 8 characters and include uppercase, lowercase, a number, and a symbol (SQL Server requirement)
- `JWT_KEY` must be at least 32 characters long

### 3. Build and start all containers

```bash
docker compose up --build
```

The first time this runs it will:

- Download base images (Node, .NET, SQL Server, Nginx) — this takes a few minutes depending on your internet connection
- Build the React app
- Build the .NET API
- Start all three containers
- Automatically apply database migrations

You will see logs from all three containers streaming in the terminal.
Wait until you see this line from the backend:

```
backend-1  | Now listening on: http://[::]:5110
```

### 4. Open the app

| What                  | URL                           |
| --------------------- | ----------------------------- |
| TaskFlow app          | http://localhost:8090         |
| Backend API / Swagger | http://localhost:5110/swagger |

Register a new account and log in. The full stack is working if you can do this successfully.

---

## Daily usage

### Start the app (after first setup)

```bash
docker compose up
```

No `--build` needed unless you changed a Dockerfile or installed new packages.

### Start in background (detached mode)

```bash
docker compose up -d
```

### View logs

```bash
# All containers
docker compose logs -f

# One container only
docker compose logs -f backend
docker compose logs -f frontend
docker compose logs -f db
```

### Stop the app

```bash
docker compose down
```

This stops and removes containers but **keeps your database data safe**.

### Stop and delete all data (full reset)

```bash
docker compose down -v
```

The `-v` flag deletes the database volume. All data will be lost. Only use this when you want a completely fresh start.

---

## Rebuilding after code changes

If you change any source code, rebuild the affected service:

```bash
# Rebuild everything
docker compose up --build

# Rebuild only the backend
docker compose up --build backend

# Rebuild only the frontend
docker compose up --build frontend
```

---

## Project structure

```
TaskFlow/
├── docker-compose.yml        # Orchestrates all containers
├── .env                      # Your secrets (never commit this)
├── .env.example              # Template for other developers
├── .gitignore                # Must include .env
│
├── Frontend/
│   ├── Dockerfile            # Node build → Nginx serve (multi-stage)
│   ├── nginx.conf            # Nginx config with /api proxy
│   ├── src/
│   └── package.json
│
└── Backend/
    ├── Dockerfile            # .NET SDK build → ASP.NET runtime (multi-stage)
    ├── TaskFlow_API.csproj
    ├── appsettings.json
    └── Program.cs
```

---

## How each Dockerfile works

### Frontend — multi-stage build

**Stage 1 (build):** Uses `node:20-alpine` to run `npm ci` and `npm run build`.
This produces a `dist/` folder of static HTML, CSS, and JS files.

**Stage 2 (serve):** Uses `nginx:alpine` (tiny image with no Node.js).
Copies only the `dist/` folder from Stage 1 into Nginx's web root.
The final image is around 25MB instead of 1GB+.

Nginx also proxies any request starting with `/api/` to the backend container,
so the browser never makes cross-origin requests.

### Backend — multi-stage build

**Stage 1 (build):** Uses `mcr.microsoft.com/dotnet/sdk:9.0` to restore packages
and publish the app in Release mode.

**Stage 2 (runtime):** Uses `mcr.microsoft.com/dotnet/aspnet:9.0` (no SDK, much smaller).
Copies only the published output from Stage 1.
The final image contains only the runtime, not the full SDK.

On startup, `Program.cs` automatically runs `db.Database.Migrate()` to apply
any pending EF Core migrations before the API starts accepting requests.

---

## Where is the database data stored?

Data is stored in a **Docker named volume** called `taskflow-project_sqlserver-data`.
It lives on your machine managed by Docker, separate from the containers.

```bash
# See all Docker volumes on your machine
docker volume ls

# Inspect where it lives
docker volume inspect taskflow-project_sqlserver-data
```

On Windows the actual files are inside WSL2:

```
\\wsl$\docker-desktop-data\data\docker\volumes\taskflow-project_sqlserver-data
```

Because it is a named volume and not inside a container, your data survives:

- `docker compose down` ✅ data safe
- `docker compose up --build` ✅ data safe
- Restarting Docker Desktop ✅ data safe
- `docker compose down -v` ❌ data deleted

---

## Environment variables reference

These are set in your `.env` file and injected by `docker-compose.yml`:

| Variable      | Used by     | Description                       |
| ------------- | ----------- | --------------------------------- |
| `DB_PASSWORD` | db, backend | SQL Server SA password            |
| `JWT_KEY`     | backend     | Secret key for signing JWT tokens |

The backend receives these as ASP.NET Core environment variables:

- `ConnectionStrings__DefaultConnection` — full SQL Server connection string built from `DB_PASSWORD`
- `Jwt__Key` — JWT signing key

The double underscore `__` maps to `:` in ASP.NET Core configuration,
so `Jwt__Key` overrides `Jwt.Key` in `appsettings.json`.

---

## Troubleshooting

### Port already in use

```
Error: port is already allocated
```

Something else on your machine is using port 8090, 5110, or 1433.
Change the left side of the port mapping in `docker-compose.yml`:

```yaml
ports:
  - "9090:80" # Change 8090 to any free port
```

### Database not ready / backend crashes on startup

SQL Server takes 30–45 seconds to be ready on first start.
The `healthcheck` in `docker-compose.yml` handles this, but if the backend
starts before the DB is healthy, restart it:

```bash
docker compose restart backend
```

### Migrations failed

Check backend logs:

```bash
docker compose logs backend
```

If you see a migration error, the database schema may be out of date.
Bring everything down and back up fresh (this deletes data):

```bash
docker compose down -v
docker compose up --build
```

### Frontend shows Nginx welcome page

Your browser has a cached response. Hard refresh with `Ctrl + Shift + R`.
If it still shows, check that port 80 is not in use by another Nginx on your machine.
The default port mapping is `8090:80` — use `http://localhost:8090`, not port 80.

### Cannot connect to Docker daemon

Docker Desktop is not running. Start it from your applications menu and wait
for the whale icon to appear in the system tray before running commands.

---

## Security notes

- Never commit `.env` to Git — it contains real passwords and secret keys
- The `.gitignore` must include `.env`
- Provide `.env.example` with placeholder values for other developers
- The JWT key in `appsettings.json` is a fallback only — always override via environment variable
- SQL Server's SA account has full admin access — use a strong password
- In production, never expose port 1433 or 5110 to the public internet

---

## Tech stack versions

| Technology            | Version          |
| --------------------- | ---------------- |
| .NET / ASP.NET Core   | 9.0              |
| Entity Framework Core | 9.0              |
| React                 | 19               |
| Vite                  | 7.x              |
| Node.js (build only)  | 20 (Alpine)      |
| Nginx                 | Alpine (latest)  |
| SQL Server            | 2022 (Developer) |
| Docker Compose        | v2               |
