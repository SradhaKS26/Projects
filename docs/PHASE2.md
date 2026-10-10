# Phase 2 — Service Catalog

## Goals

Make the platform's catalog fully manageable without code changes:

- Admin CRUD for service categories and services
- Base pricing per service
- Catalog browsing for customers and providers (mobile + API)
- Sample seed catalog so a fresh environment is usable

## Design decisions

1. **Reads are open to any authenticated user, writes require `ManageServices`.**
   Customers and providers need to browse the catalog to book anything, so gating
   reads behind an admin permission would break the customer flow. Writes stay
   admin-only.

2. **`includeInactive` is a request, not a guarantee.** Both controllers narrow
   `includeInactive` to `includeInactive && caller has ManageServices`. A customer
   passing `?includeInactive=true` still only sees active records, so the flag can
   never be used to enumerate retired services or draft pricing.

3. **An inactive category hides its services.** A service is only visible to
   non-admins when both the service and its category are active. Deactivating a
   category also deactivates its services in the same transaction, so the two
   representations cannot drift.

4. **Services are soft-deleted once booked.** `DELETE /api/services/{id}` hard-deletes
   only when no `ServiceRequest` references the service; otherwise it deactivates.
   This keeps historical requests readable without orphaning foreign keys.

5. **Categories are never hard-deleted.** `DELETE /api/service-categories/{id}`
   always deactivates. Categories are referenced widely enough that deletion is not
   worth the failure modes at MVP scale.

6. **Duplicate names are rejected at two levels.** The service layer returns a `409`
   with a readable message, and a unique index on `(CategoryId, Name)` backstops it
   against races. Category names are unique globally (index added in Phase 1).

## Schema change

One migration, `AddServiceNameUniqueIndex`: replaces the plain `IX_Services_CategoryId`
index with a unique `IX_Services_CategoryId_Name`. Apply with:

```bash
cd backend
dotnet ef database update --project src/ServiceManagement.Infrastructure --startup-project src/ServiceManagement.Api
```

The API also runs migrations on startup, so a normal `dotnet run` picks this up.

> If you already created two services with the same name in one category, the
> migration will fail until you rename or remove one of them.

## API endpoints (Phase 2)

| Method | Path | Authorization |
|--------|------|---------------|
| GET | `/api/service-categories` | Any authenticated user |
| GET | `/api/service-categories/{id}` | Any authenticated user |
| POST | `/api/service-categories` | `ManageServices` |
| PUT | `/api/service-categories/{id}` | `ManageServices` |
| DELETE | `/api/service-categories/{id}` | `ManageServices` (deactivates) |
| GET | `/api/services` | Any authenticated user |
| GET | `/api/services/{id}` | Any authenticated user |
| POST | `/api/services` | `ManageServices` |
| PUT | `/api/services/{id}` | `ManageServices` |
| DELETE | `/api/services/{id}` | `ManageServices` |

`GET /api/services` accepts `categoryId`, `search`, and `includeInactive` query
parameters. `search` matches name or description, case-insensitively.

## Validation pipeline fix

Phase 1 registered FluentValidation validators in DI but never invoked them, so
requests like a negative `BasePrice` reached the service layer unchecked. A
`ValidationFilter` now runs any registered validator against action arguments and
throws `ValidationException`, which the existing exception middleware already maps
to a `400` with the standard response envelope. This applies retroactively to the
Phase 1 auth validators as well.

## Clients

**Angular admin** gains two screens, replacing the Phase 1 placeholders:

- `/service-categories` — table with create/edit dialogs, a "show inactive" toggle,
  and a service count per category.
- `/services` — table with create/edit dialogs, debounced search, category filter,
  and a "show inactive" toggle.

**Flutter** replaces the Phase 2 placeholder with a category list that drills into
the services for that category, using the same read endpoints.

## Seed data

`DbSeeder` seeds four generic categories (Home Cleaning, Cooking, Delivery,
Repairs & Maintenance) with ten services between them, but only when the catalog is
empty. It never overwrites operator-managed data.

## Not in this phase

Provider-to-service mapping, per-provider pricing overrides, and the service request
lifecycle. `ProviderService` and `ServiceRequest` remain modeled but unused until
Phase 3A / 3B.

## Next phases

- Phase 3A: provider onboarding and approval — see [PHASE3A.md](PHASE3A.md)
- Phase 3B: service request broadcast, first-to-accept, expiry
- Phase 4: provider workflows + notifications
- Phase 5: customer tracking, history, reviews
