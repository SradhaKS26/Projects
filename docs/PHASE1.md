# Phase 1 — Foundation

## Goals

Deliver a low-cost MVP foundation:

- Modular monolith ASP.NET Core API
- PostgreSQL + EF Core
- JWT authentication with ASP.NET Core Identity
- Centralized RBAC (roles + permissions)
- Angular admin shell
- Flutter mobile shell
- Critical auth/authorization tests

## Assumptions

1. Monorepo layout: `backend/`, `admin-web/`, `mobile/`.
2. Self-registration is limited to `CommonUser` and `ServiceProvider`. Administrators are seeded.
3. Permission checks are claim-based (`permission` claims in JWT) via authorization policies.
4. Service catalog / request workflow entities are modeled now; full CRUD arrives in later phases.
5. No Redis, Kafka, SignalR, payments, or live GPS in Phase 1.

## Backend layout

```text
backend/
  src/
    ServiceManagement.Api/
    ServiceManagement.Application/
    ServiceManagement.Domain/
    ServiceManagement.Infrastructure/
  tests/
    ServiceManagement.UnitTests/
    ServiceManagement.IntegrationTests/
```

## API endpoints (Phase 1)

| Method | Path | Notes |
|--------|------|-------|
| GET | `/api/health` | Anonymous health check |
| POST | `/api/auth/register` | CommonUser / ServiceProvider |
| POST | `/api/auth/login` | Returns access + refresh tokens |
| POST | `/api/auth/refresh` | Rotates refresh token |
| GET | `/api/users/me` | Current user |
| GET | `/api/users` | Requires `ManageUsers` |
| GET/PUT | `/api/users/{id}` | Self or `ManageUsers` |
| GET | `/api/roles` | Requires `ManageRoles` |
| GET | `/api/permissions` | Requires `ManagePermissions` |
| PUT | `/api/roles/{id}/permissions` | Requires `ManagePermissions` |

## Local run

```bash
docker compose up -d postgres
cd backend
dotnet ef database update --project src/ServiceManagement.Infrastructure --startup-project src/ServiceManagement.Api
dotnet run --project src/ServiceManagement.Api

cd ../admin-web
npm start
```

Postgres is published on host port **5433** (not 5432) so it does not conflict with a local Windows PostgreSQL install.


Default seeded admin (change immediately in any shared environment):

- Email: `admin@servicemanagement.local`
- Password: `ChangeMe!Admin123`

## Next phases

- Phase 2: service categories, services, pricing, admin CRUD, Flutter browsing — see [PHASE2.md](PHASE2.md)
- Phase 3: service request lifecycle and assignment
- Phase 4: provider workflows + notifications
- Phase 5: customer tracking, history, reviews
