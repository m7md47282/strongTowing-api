# Payment Implementation Test Plan

## Backend Verification

- `POST /api/orders` with `PayLater` creates:
  - job with `paymentStatus = Pending` (or `PendingCash` when method is cash),
  - linked payment record with expected amount.
- `POST /api/orders` with `PayNow` returns `clientSecret`, `paymentIntentId`, and publishable key with pre-authorization metadata.
- High-risk order intake sets `paymentStatus = UnderReview` and requires fraud review decision.
- `GET /api/payments/fraud-review-queue` returns only review-queued records.
- `POST /api/payments/{id}/review-decision`:
  - `approve` resumes payment progression,
  - `reject` cancels payment/job progression.
- Stripe manual capture flow:
  - webhook `payment_intent.amount_capturable_updated` sets payment `Authorized`,
  - `POST /api/payments/{id}/capture-authorization` sets payment `Paid`,
  - `POST /api/payments/{id}/release-authorization` releases hold and sets `Cancelled`.
- `POST /api/payments/{id}/mark-cash-pending` transitions payment/job to `PendingCash`.
- `POST /api/payments/{id}/mark-cash-collected` transitions payment/job to `Paid` and sets collection metadata.
- `POST /api/payments/{id}/cancel` transitions unsettled payment to `Cancelled`.
- `POST /api/jobs/{id}/cancel-with-fee` applies stage-based fee matrix:
  - before dispatch,
  - after dispatch,
  - after arrival.
- cancellation with preauth hold captures fee amount when fee > 0; releases hold when fee = 0.
- `PUT /api/jobs/{id}/status` to `Completed` fails if payment is not settled.
- `POST /api/jobs/{id}/complete-with-override` succeeds for Admin/SuperAdmin with reason and writes audit note.

## Frontend Verification (Angular 19)

- `request-service` page is accessible without authentication.
- Guest can submit request with:
  - `PayLater` + `PaymentLink` and receive success message,
  - `PayLater` + `Cash` and record pending cash flow.
- Guest can select `PayNow` and trigger Stripe payment initialization path.
- Guest sees pre-authorization hold disclosure and receives authorized-state success feedback.
- Dispatcher payments page:
  - displays `UnderReview`, `Authorized`, `CapturePending`, `PendingCash`, and `Cancelled` statuses,
  - can switch `Pending -> PendingCash`,
  - can confirm `PendingCash -> Paid`,
  - can approve/reject fraud-review queue items,
  - can capture/release authorization holds,
  - can cancel unsettled payments.
- Customer cancellation flow shows policy warning before cancellation action.
- Financial report includes cancellation-fee revenue and status counts for review/authorized/cancelled.

## Rollout / Feature-Flag Verification

- Start with `PreAuthorizationEnabled = false` and validate legacy flows still pass.
- Enable preauth and verify:
  - auth-only does not unlock completion,
  - capture unlocks completion.
- Configure cancellation fee matrix and verify stage-specific percentages in UAT.
- Monitor fraud queue volume and false-positive rate for first 7 days.

## Commands Run

- Backend build: `dotnet build` in `api/StrongTowing.API`
- Frontend build: `npm run build` in `fontend/towing-web`
