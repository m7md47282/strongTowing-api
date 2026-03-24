# Payment Flow Diagram (Client Approval)

This diagram describes the agreed payment process before implementation.

## End-to-End Flow

```mermaid
flowchart TD
    A[Customer Requests Service] --> B{Request Source}

    B -->|Phone Call| C[Dispatcher Creates Job]
    B -->|Web App Guest Page (No Login)| D[Customer Creates Job]

    C --> E[System Calculates Job Price]
    D --> E

    E --> F{When to Pay?}
    F -->|Pay Now| G[Immediate Payment]
    F -->|Pay Later| H[Create Job with Status: Payment Pending]

    %% Pay Now branch
    G --> I{Who Processes Payment?}
    I -->|Customer| J[Customer Pays in App by Card]
    I -->|Dispatcher| K[Dispatcher Charges Card on Behalf of Customer]
    J --> L{Stripe Result}
    K --> L
    L -->|Success| M[Payment Status = Paid]
    L -->|Fail| N[Payment Status = Failed]
    N --> O[Retry or Switch Method]
    O --> P{Selected Method}
    P -->|Payment Link| Q[Dispatcher Sends Payment Link]
    P -->|Card Retry| G
    P -->|Cash| R[Mark as Cash / PendingCash]

    %% Pay Later branch
    H --> S[Dispatcher Continues Job Process]
    S --> T[Dispatcher Contacts Customer and Agrees Payment Method]
    T --> U{Payment Method}
    U -->|Payment Link| Q
    U -->|Card by Dispatcher| K
    U -->|Cash at Location| R

    %% Link + Cash branches
    Q --> V[Customer Pays from Stripe Link]
    V --> W{Stripe Result}
    W -->|Success| M
    W -->|Fail| N

    R --> X[Cash Collected and Confirmed]
    X --> M

    %% Final path
    M --> Y[Dispatcher Assigns/Continues Service]
    Y --> Z{Job Completion Check}
    Z -->|Payment Settled| AA[Complete Job]
    Z -->|Payment Not Settled| AB[Hold Final Completion Until Settlement or Admin Override]
```

## Payment Statuses

- `Pending`: Job created, payment not completed yet (Pay Later).
- `PendingCash`: Cash selected, waiting for collection confirmation.
- `Paid`: Payment completed successfully.
- `Failed`: Payment attempt failed.
- `Refunded`: Payment reversed (full/partial).
- `Cancelled`: Job/payment cancelled.

## Approval Points (Client Sign-Off)

Please confirm these points before development:

1. Customer can create a job as guest (no login).
2. Customer can choose **Pay Now** or **Pay Later**.
3. Pay Later creates job with **Payment Pending** status.
4. Dispatcher can finalize payment later via:
   - Payment link
   - Card on behalf of customer
   - Cash collection
5. Job final completion requires payment settlement unless admin override.

## Version

- Draft for approval
- Date: 2026-03-04
