# Phase 3A — Provider Onboarding and Approval

## Goals

Providers can apply to fulfill catalog services. Administrators review the
application and documents in the Angular portal and approve, reject (with a
reason), or later suspend. Until approval, a provider who logs in sees only
their application status.

## Product model (already decided)

Vehicle types are ordinary **Services**, not a new concept:

- Category `Taxi`
- Services `Auto`, `Sedan`, `SUV`, each with its own `BasePrice`
- Pricing is per service (vehicle type), never per driver

The platform stays service-agnostic. Those names exist only as seed data. There
is no taxi-specific branch in application code.

## Design decisions

1. **Approval status is its own field.** `ServiceProviderProfile.ApprovalStatus`
   is `Pending | Approved | Rejected | Suspended`. It is independent of:
   - `IsActive` — operational kill switch (an approved provider can be deactivated
     without changing approval)
   - `AvailabilityStatus` — the provider's current willingness to receive work
     (`Unavailable | Available | Busy`)

2. **`ProviderService` is an application.** A provider selects catalog services
   they can fulfill. Each row has `Pending | Approved | Rejected`. Approving the
   profile also approves pending service applications. Pricing stays on
   `Service.BasePrice`.

3. **Required documents are configurable per category.** A cook needs a
   food-safety certificate; a driver needs a licence. Administrators manage
   `CategoryDocumentRequirement` rows on each category. The onboarding UI loads
   requirements from the API, so adding a category never needs a code change.

4. **Files are not in PostgreSQL.** `ProviderDocument` stores metadata and a
   storage key. Bytes go through `IFileStorage` (local disk in Phase 3A) behind
   an authenticated download endpoint. Swap the implementation for object storage
   later without changing the schema.

5. **Eligibility is a single domain rule.** A provider receives requests for a
   service only when all of these hold:
   - `ApprovalStatus == Approved`
   - `IsActive`
   - `AvailabilityStatus == Available`
   - registered for that service (`ProviderService.Status == Approved`)

   `ProviderEligibility` encodes this for Phase 3B broadcast. There is no
   request workflow in this phase.

## Schema change

Migration `AddProviderOnboarding`:

- `ServiceProviderProfiles`: `ApprovalStatus`, `ReviewReason`, `ReviewedAt`,
  `ReviewedByUserId`
- `ProviderServices`: `Status`, `AppliedAt`, `ReviewedAt`
- `CategoryDocumentRequirements`
- `ProviderDocuments` (unique per provider + requirement; unique `StorageKey`)

Apply with:

```bash
cd backend
dotnet ef database update --project src/ServiceManagement.Infrastructure --startup-project src/ServiceManagement.Api
```

The API also runs migrations on startup.

## API endpoints (Phase 3A)

| Method | Path | Authorization |
|--------|------|----------------|
| GET | `/api/providers/me` | Authenticated provider |
| PUT | `/api/providers/me/application` | Authenticated provider |
| PUT | `/api/providers/me/availability` | Authenticated provider (approved + active) |
| POST | `/api/providers/me/documents` | Authenticated provider (multipart) |
| DELETE | `/api/providers/me/documents/{id}` | Authenticated provider (while pending/rejected) |
| GET | `/api/providers` | `ManageProviders` |
| GET | `/api/providers/{id}` | `ManageProviders` |
| POST | `/api/providers/{id}/approve` | `ManageProviders` |
| POST | `/api/providers/{id}/reject` | `ManageProviders` (reason required) |
| POST | `/api/providers/{id}/suspend` | `ManageProviders` (reason required) |
| POST | `/api/providers/{id}/reinstate` | `ManageProviders` |
| PUT | `/api/providers/{id}/active` | `ManageProviders` |
| GET | `/api/provider-documents/{id}/file` | Owner or `ManageProviders` |
| GET | `/api/service-categories/{id}/document-requirements` | Any authenticated user |
| POST | `/api/service-categories/{id}/document-requirements` | `ManageServices` |
| PUT | `/api/document-requirements/{id}` | `ManageServices` |
| DELETE | `/api/document-requirements/{id}` | `ManageServices` |

Documents: PDF, JPEG, PNG, or WebP, max 10 MB. Stored under
`FileStorage:LocalRoot` (default `storage/provider-documents`).

## Clients

**Angular admin** replaces the Providers placeholder:

- `/providers` — filterable queue with approval status and missing-docs flag
- `/providers/:id` — review documents, applied services, approve / reject /
  suspend / reinstate, and the operational `IsActive` toggle
- Categories gain a document-requirements dialog
- Unapproved providers who log in see only `/my-application`

Also in this phase (known bugs):

- `isAuthenticated()` decodes the JWT `exp` claim so an expired session redirects
  to login instead of a dashboard that 401s
- A 401 interceptor retries `/api/auth/refresh` once, then logs out
- Catalog create/edit/delete buttons render only when the user has `ManageServices`

**Flutter** stores the user from login/register. Unapproved providers are sent to
`/provider/application` (status, service selection, document upload). Customers
keep the existing catalog browse flow.

## Seed data

In addition to the Phase 2 generic catalog, a fresh (or existing) database gets:

- Category **Taxi** with services **Auto**, **Sedan**, **SUV** and base prices
- Document requirements on Taxi (licence, registration, insurance) and Cooking
  (food-safety certificate), as examples of per-category configuration
- Demo provider `provider@test.local` / `Provider!123` with a **Pending**
  application for Auto and Sedan, ready for admin review

Seeders never overwrite operator-managed catalog rows; Taxi and requirements are
added only when missing.

## Not in this phase

Service request broadcast, first-to-accept claiming, hidden contact details, and
unaccepted-request expiry. Those are Phase 3B. The concurrent-accept race must
be a single atomic conditional update, not read-then-write.

## Next phases

- Phase 3B: customer request broadcast to eligible providers, claim, expiry
- Phase 4: provider workflows + notifications
- Phase 5: customer tracking, history, reviews
