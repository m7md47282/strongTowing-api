# Payment Workflow Reference

## Purpose
This document defines the end-to-end payment workflow for the towing system so the team can implement one consistent business flow across dispatcher and customer scenarios.

---

## Payment Methods
The system supports 3 payment methods:

1. **Stripe - Payment Link**
   - Dispatcher creates a payment link and sends it to the customer.
   - Customer pays online using Stripe Checkout.

2. **Stripe - Direct Card Payment**
   - Payment is made immediately inside the web app.
   - Can be done by:
     - Customer on their own request flow, or
     - Dispatcher on behalf of the customer (using saved card or new card).

3. **Cash**
   - Customer pays cash in person at service location.
   - Dispatcher/driver marks payment as cash in the system.

---

## Core Business Rule
Payment is always linked to a specific job.

- Job has calculated price (based on service type, distance, and other pricing factors).
- That price becomes the target payment amount.
- A job should track payment method, payment status, and payment record/history.

---

## Main Scenario A: Dispatcher Creates Job

### Step-by-step
1. Customer calls dispatcher and requests service.
2. Dispatcher collects job input data (service type, pickup/drop, distance, vehicle info, etc.).
3. System calculates estimated/final job price.
4. Dispatcher creates the job with that price.
5. Dispatcher chooses one payment path:

   **A1) Send Payment Link**
   - Dispatcher generates payment link for the job amount.
   - Link is sent to customer (SMS/email/WhatsApp/manual sharing).
   - Customer opens link and pays online.
   - System marks payment as `Paid` and links transaction to job.

   **A2) Pay by Card on Behalf of Customer**
   - Dispatcher charges saved card from customer profile, or enters a new card.
   - Stripe processes charge.
   - On success, system marks payment as `Paid`.

   **A3) Mark as Cash**
   - Customer chooses cash at location.
   - System marks method as `Cash` and status as `PendingCash` (or `Unpaid` until collected).
   - When cash is collected, status changes to `Paid`.

6. Dispatcher proceeds with dispatch flow (assign driver, track status, complete job).

---

## Main Scenario B: Customer Creates Job (No Login, Pay Now or Later)

### Step-by-step
1. Customer opens public service request page (guest flow, no login).
2. System calculates and shows price before confirmation.
3. Customer enters required contact details (name, phone, and optional email).
4. Customer chooses one of two options:
   - **Pay Now**: pay directly in app using card (Stripe).
   - **Pay Later**: submit request without immediate payment.
5. System creates the job in both cases:
   - If **Pay Now** succeeds -> payment status is `Paid`.
   - If **Pay Later** -> payment status is `Pending` (display label: "Payment Pending").
6. Dispatcher is notified about the new request and continues operations (contact customer, assign driver, monitor job, complete service).
7. Dispatcher updates payment later based on agreement with customer:
   - Send payment link,
   - Charge card on behalf of customer, or
   - Confirm cash collection.

---

## Payment Status Lifecycle (Recommended)

Use a clear and consistent status lifecycle:

- `Unpaid` - Job created, no payment attempt yet.
- `Pending` - Payment pending; job exists but payment is not completed yet.
- `PendingCash` - Cash selected, waiting for collection.
- `Paid` - Payment completed successfully.
- `Failed` - Payment attempt failed.
- `Cancelled` - Job/payment cancelled.
- `Refunded` - Payment was refunded (full or partial).

---

## Job + Payment Data to Track

For each job/payment, keep these minimum fields:

- `jobId`
- `amount`
- `currency`
- `paymentMethod` (`Card`, `PaymentLink`, `Cash`)
- `paymentStatus`
- `paymentDueMode` (`PayNow`, `PayLater`)
- `transactionId` (Stripe intent/charge/session ID when applicable)
- `paymentLinkUrl` (if method is payment link)
- `paidAt`
- `paidBy` (customer/dispatcher/system user id)
- `createdBy` (dispatcher/customer/guest)
- `guestName` (if guest flow)
- `guestPhone` (if guest flow)
- `guestEmail` (optional, if guest flow)

Optional but recommended:
- `failureReason`
- `refundAmount`
- `refundReason`
- `notes`

---

## System Events / Notifications

Recommended notifications:

- **Dispatcher notified** when:
  - payment link is paid,
  - customer creates request and pays directly in app,
  - customer creates request with pay-later (`Pending`) status.
- **Customer notified** when:
  - payment link is created,
  - payment succeeds/fails,
  - refund is issued.
  - For guest flow, notifications are sent by SMS/email using submitted guest contact details.

---

## Validation Rules

1. Payment amount must match job price unless admin override is explicitly used.
2. Job can proceed while payment is `Pending`, but final completion/release should require settlement unless admin override is explicitly allowed.
3. Stripe webhook confirmation should be source of truth for online payment success.
4. Cash payments require explicit collection confirmation step.
5. Every payment action must be audit-logged (who, when, amount, method, result).
6. Guest requests must still create a unique customer reference (guest id/token) for tracking and support.

---

## Integration Mapping (High Level)

Typical API interactions:

1. Create/price job request
2. Create job
3. Choose payment method:
   - Card: create/confirm payment intent
   - Link: create checkout session/link
   - Cash: mark cash-selected
4. If customer selected pay-later, keep payment status as `Pending` and continue dispatcher workflow
5. Update payment status from webhook/manual confirmation
6. Continue or finalize job operations after payment state update

---

## Example End-to-End Flows

### Example 1: Dispatcher + Payment Link
- Job price = $150
- Dispatcher creates payment link
- Customer pays online
- Payment status -> `Paid`
- Dispatcher assigns driver and executes job

### Example 2: Dispatcher + Card on Profile
- Job price = $220
- Dispatcher charges saved customer card
- Stripe returns success
- Payment status -> `Paid`
- Job continues

### Example 3: Customer Self-Service + Direct Card
- Customer opens public request page (no login)
- Customer enters contact details
- Customer pays $180 immediately
- System notifies dispatcher of new paid job
- Dispatcher contacts customer and proceeds

### Example 4: Customer Self-Service + Pay Later
- Customer opens public request page (no login)
- Customer enters contact details and submits without immediate payment
- Job is created with status `Pending` ("Payment Pending")
- Dispatcher contacts customer and agrees on payment method
- Dispatcher updates payment later (link/card/cash) and status becomes `Paid`

### Example 5: Cash at Location
- Job price = $130
- Method set to `Cash`
- Status `PendingCash` until collection
- After driver/dispatcher confirms collection -> `Paid`

---

## Open Items (To Finalize Later)

1. Final pricing formula inputs and rounding rules.
2. When to allow unpaid jobs to proceed in special cases.
3. Link expiration time and retry behavior.
4. Refund permissions by role (dispatcher/admin/superadmin).
5. Partial payment or deposits (if needed in future).

---

## Implementation Summary
The system should treat payment as a first-class part of job lifecycle with three methods: **Stripe payment link**, **Stripe direct card**, and **cash**. It must support both dispatcher-led and customer self-service flows while keeping status tracking, audit logs, and notifications consistent.
