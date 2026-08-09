# Service Management Platform

Startup MVP for **service management and service fulfillment**.

Customers discover and request services. Providers accept and fulfill requests. Administrators manage the platform through a web portal.

## Architecture

```text
Flutter Mobile App
        |
        | HTTPS / REST
        v
Angular Admin Web -----> ASP.NET Core Web API -----> PostgreSQL
```

Clients never access the database directly. Business logic and authorization live in the API.

## Repository layout

| Path | Purpose |
|------|---------|
| `backend/` | ASP.NET Core modular monolith |
| `admin-web/` | Angular administrator portal |
| `mobile/` | Flutter app (customers + providers) |
| `docs/` | Architecture and phase notes |
| `docker-compose.yml` | Local PostgreSQL |

## Tech stack

- **Backend:** ASP.NET Core 8, EF Core, PostgreSQL, Identity + JWT, FluentValidation, Serilog, Swagger
- **Admin web:** Angular 19, Angular Material, Reactive Forms, RxJS
- **Mobile:** Flutter, Riverpod, Dio, secure token storage
- **Infra (MVP):** one API + one PostgreSQL database + static Angular hosting later

## Phase 1 status

Completed foundation:

- Solution structure and domain model
- Auth (register/login/refresh)
- Roles + permissions RBAC
- Angular admin shell (login, dashboard, users)
- Flutter shell (login/home/services/profile)
- Unit + API integration tests for auth/RBAC

See [docs/PHASE1.md](docs/PHASE1.md).

## Quick start

### 1. Database

```bash
docker compose up -d postgres
```

### 2. API

```bash
cd backend
dotnet restore
dotnet run --project src/ServiceManagement.Api
```

API: `http://localhost:5080`  
Swagger: `http://localhost:5080/swagger`

Seeded admin:

- `admin@servicemanagement.local`
- `ChangeMe!Admin123`

### 3. Admin web

```bash
cd admin-web
npm install
npm start
```

App: `http://localhost:4200`

### 4. Mobile

```bash
cd mobile
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5080/api
```

## Security notes

- Secrets belong in environment variables / secret stores, not source control.
- Change the JWT signing key and seeded admin password before any shared deployment.
- Authorization is enforced server-side with role/permission policies.

## Product principle

The platform is category-agnostic:

`ServiceCategory -> Service -> ServiceRequest -> ServiceProvider`

Administrators can add new service types without code changes (Phase 2+).
