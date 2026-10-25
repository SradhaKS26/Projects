# Phase 4 — Provider workflow and notifications

## Goals

After a provider accepts a request they start the job, then complete it (or mark
it failed). Customers see the status on Requests. Status changes write an in-app
notification. Clients poll; there is still no SignalR.

## Design decisions

1. **State machine is unchanged.** `Accepted → InProgress → Completed|Failed`.
   Customers may still cancel while Accepted or InProgress.

2. **Assigned list is separate from inbox.** Inbox stays pending broadcast.
   `GET /api/service-requests/assigned` is the provider’s claimed jobs.

3. **Only the assigned provider** may start, complete, or fail.

4. **Availability.** Accept sets Busy. Complete, fail, or customer cancel
   restores Available when the provider has no other Accepted/InProgress jobs.

5. **Notifications are rows, not push.** `UserNotifications` per user. Flutter
   Home shows Updates; pull-to-refresh reloads them.

## API

| Method | Path | Who |
|--------|------|-----|
| GET | `/api/service-requests/assigned` | `ServiceProvider` |
| POST | `/api/service-requests/{id}/start` | Assigned provider |
| POST | `/api/service-requests/{id}/complete` | Assigned provider |
| POST | `/api/service-requests/{id}/fail` | Assigned provider |
| GET | `/api/notifications` | Authenticated user |
| POST | `/api/notifications/{id}/read` | Owner |
| POST | `/api/notifications/read-all` | Authenticated user |

## Flutter

- Requests: provider **Your jobs** with Start / Complete / Could not complete
- Home: **Updates** list
- Customer requests still show status and Cancel while allowed

## Not in this phase

Live map tracking, email/SMS/push. Status-history tracking and customer
reviews are Phase 5 — see [PHASE5.md](PHASE5.md).
