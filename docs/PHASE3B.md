# Phase 3B — Request broadcast and first accept

## Goals

Customers place a request for a catalog service. Eligible providers see it and
the first valid accept wins. Contact details stay hidden until then. Unaccepted
requests expire after 10 minutes.

Pending providers keep the normal Home / Services / Requests / Profile tabs. Their
onboarding status lives on the **Application** tab.

## Design decisions

1. **Eligibility is unchanged from Phase 3A.** A provider is offered a request
   only when approved, active, Available, and registered (`ProviderService`
   approved) for that service.

2. **First accept is one SQL UPDATE.** PostgreSQL uses
   `UPDATE ... WHERE Status = Pending AND AssignedProviderId IS NULL AND ExpiresAt > now()`.
   Two concurrent accepts cannot both succeed.

3. **Contacts and street address are hidden until Accepted.** Inbox cards show
   service, price, notes, and expiry only. After accept, both sides see name,
   email, phone, and address.

4. **Expiry is 10 minutes** (`RequestBroadcast:ExpiryMinutes`). Pending rows
   become `Expired` on read and via a 30-second hosted sweep (disabled in tests).

5. **No SignalR.** Clients poll / pull-to-refresh the Requests tab.

6. **Customers place requests; providers accept.** Providers do not place
   catalog requests in this phase.

## API

| Method | Path | Who |
|--------|------|-----|
| POST | `/api/service-requests` | `CommonUser` |
| GET | `/api/service-requests/mine` | Authenticated customer |
| GET | `/api/service-requests/inbox` | `ServiceProvider` |
| GET | `/api/service-requests/{id}` | Owner, assignee, or eligible provider |
| POST | `/api/service-requests/{id}/accept` | Eligible provider |
| POST | `/api/service-requests/{id}/cancel` | Owning customer, while pending |

## Flutter

- **Request** on a service card (customers) and a Home shortcut
- **Requests** tab: customer list + cancel; provider inbox + accept
- **Application** tab (providers only): onboarding status, even while pending

## Not in this phase

Live tracking and reviews (Phase 5). In-progress / complete and in-app
notifications are Phase 4 — see [PHASE4.md](PHASE4.md).
