# Strong Towing — What We Have & What We Need

This document summarizes **capabilities already implemented** in this repository versus **common gaps** compared to full-featured commercial towing / dispatch platforms (e.g. municipal rotation, motor club networks, impound yards). Use it for roadmap planning; it is not a contract or SLA.

---

## What we have

### Core operations

- **Job lifecycle** with statuses: waiting → dispatch → on route → on scene → loaded → completed (or cancelled).
- **Dispatcher and admin** job management: create, assign drivers, update status, filters and search (see dispatcher/admin flows in the Angular app).
- **Driver experience** (web): job list, job detail, profile, earnings, availability for dispatch.
- **Customer area** and **public request flow**: guest order creation and payment initialization (API + frontend).

### Vehicles & pricing

- **Vehicles** linked to owners (users), VIN and vehicle attributes; **NHTSA-style vehicle catalog** integration for make/model data.
- **Service pricing profiles** and **insurance accounts** with zone-style rates and service charges.
- **Google Routes** integration: office location, driving distance/duration from office to client coordinates.

### Billing & payments

- **Job cost**, line-item charges (stored with the job), **Stripe payment links**, multiple payment methods and statuses.
- **Split billing**: insurance vs client portions, flags for billed/paid portions, cash-to-driver payroll deduction flows.
- **Payroll-related**: driver payroll records, payroll reports (admin), financial summary and **CSV export**.

### Communication & notifications

- **SMS** (e.g. Twilio) and **email** (e.g. Postmark) with **admin-configurable toggles** per event (job created, driver assigned, status changes, payment events, etc.).
- **Firebase Cloud Messaging** for browser push; token register/unregister endpoints.

### Location

- **Driver GPS ping**: last known latitude/longitude and timestamp on the user record.
- **Route calculation** endpoint for distance/duration (not full continuous breadcrumb tracking).

### Auth & users

- **JWT** auth, refresh tokens, **role-based** access (e.g. super admin, admin, dispatcher, driver, customer).
- **OTP** flows for signup and password reset; **pending driver signup** support where implemented.

### Other

- **Job photos** (upload/storage URLs tied to jobs).
- **System settings** (including email/SMS provider configuration) in admin settings.
- **Documentation** elsewhere in the repo: dispatcher guide, payment flows, pricing/calculation notes, API docs.

---

## What we need (gaps vs. many full towing systems)

Prioritize by **your** business (retail-only vs. yard vs. municipal vs. motor club). Not every company needs everything below.

### High impact for specific business models

| Area | Gap |
|------|-----|
| **Impound / storage / lien** | No first-class impound lot, storage billing, lien workflow, auction/disposal, or release authorization chain. |
| **Police / municipal rotation** | No rotation lists, contract zones, or compliance reporting for city/county programs. |
| **Motor club / insurer dispatch APIs** | No automated inbound jobs or outbound status/charges from third-party dispatch networks (AAA-style programs, etc.). These are partner-specific integrations. |

### Fleet, compliance, and field operations

| Area | Gap |
|------|-----|
| **Fleet assets** | Jobs may reference a `TruckId` as text; no full truck/equipment records (maintenance, inspections, DOT numbers, capacity). |
| **Driver compliance** | No built-in CDL/medical card/expiry tracking on the user model as a full compliance module. |
| **GPS depth** | Last ping only—not full track history, geofencing, or customer-facing live ETA from continuous telemetry. |
| **Native mobile app** | Driver/dispatcher experience is web-based; native apps often add background GPS and offline resilience. |

### Data model and back office

| Area | Gap |
|------|-----|
| **Structured addresses** | Pickup/destination are largely free text; many systems normalize geocoded addresses for routing, tax, and reporting. |
| **Accounting integration** | CSV/financial summaries exist; **QuickBooks / Xero**-class sync and AR aging are not built in. |
| **Multi-location / franchise** | Single-tenant style; no org hierarchy for multi-yard or franchise operators. |

### CRM and B2B depth

| Area | Gap |
|------|-----|
| **Commercial CRM** | Accounts and insurance profiles exist; deeper pipelines, SLAs, and contract pricing tiers are optional enhancements. |

---

## Glossary: motor club API

A **motor club API** is a **partner integration**: the roadside program’s system sends jobs to your software and receives status/completion/charges electronically, instead of manual re-entry from a portal or phone. Each club defines its own API or legacy format; implementation is **per partner**, not one generic plug-in.

---

## Related docs in this repo

- `fontend/towing-web/DISPATCHER_DOCUMENTATION.md` — dispatcher workflows and job data expectations.
- `api/StrongTowing.API/API_DOCUMENTATION.md` — API overview.
- `fontend/towing-web/PAYMENT_WORKFLOW_REFERENCE.md` — payment behavior (frontend-oriented).
- `api/StrongTowing.API/PRICING_CALCULATION_GUIDE.md` — pricing logic.

---

*Last updated: 2026-04-10 — align this file when major features ship.*
