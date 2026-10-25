# Phase 5 — Tracking, history, and reviews

## Goals

After a job is completed, the customer rates the provider. Both sides can see a
status timeline (tracking without GPS). Past requests stay on Requests as
history. The provider’s average rating is stored on their profile.

This is the last slice of the original V1 marketplace loop:

`Request → Accept → Start → Complete → Rating`

## Design decisions

1. **Reviews use the existing `Reviews` table.** One review per request
   (`ServiceRequestId` unique). Rating is 1–5. Comment is optional.

2. **Only the owning customer** may review, and only when status is
   `Completed`. Failed, cancelled, and expired jobs are history, not reviews.
   Provider-to-customer ratings are deferred.

3. **Tracking is the status history**, not live maps. `ServiceRequestStatusHistories`
   is returned as `timeline` on owned/assigned jobs. GPS / Redis geo stay out.

4. **Provider rating is a cached average** on `ServiceProviderProfiles.Rating`
   (precision 3,2). `ReviewCount` is computed on read.

5. **Notifications stay in-app.** Completing a job asks the customer to rate.
   Submitting a review notifies the provider. Still no SignalR, email, or SMS.

## API

| Method | Path | Who |
|--------|------|-----|
| POST | `/api/service-requests/{id}/review` | Owning `CommonUser`, completed job |
| GET | `/api/reviews/received` | `ServiceProvider` |
| GET | `/api/service-requests/mine` | Includes `timeline`, `review`, `providerRating` |
| GET | `/api/service-requests/assigned` | Same extras after accept |

`POST /review` body:

```json
{ "rating": 5, "comment": "On time and thorough." }
```

## Flutter

- Requests: **Your requests** (open) and **History** (terminal). Completed
  history cards show **Rate provider** once.
- Job cards expand **Track status** from the timeline.
- Profile: customer history counts; provider average + recent reviews.
- Application: approved providers see their customer rating.

## Admin

Provider list and review screens show the cached customer rating when any
reviews exist.

## Not in this phase

Payments, live GPS tracking, chat, Redis, email/SMS/push, cancellation
refunds, and provider-to-customer ratings.
