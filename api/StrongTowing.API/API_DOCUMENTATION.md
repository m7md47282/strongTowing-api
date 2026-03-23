# StrongTowing API Documentation

## Swagger JSON File

The complete API specification is available in `swagger.json`. This file can be:

1. **Imported into Postman** - Use "Import" → "File" → Select `swagger.json`
2. **Viewed in Swagger UI** - When the API is running, visit `/swagger`
3. **Used with OpenAPI tools** - Any tool that supports OpenAPI 3.0
4. **Shared with Frontend Team** - Direct JSON file for reference

## Quick Start for Frontend Team

### Base URLs
- **Development**: `http://localhost:5155/api`
- **Production**: `https://yourdomain.com/api` (update when deployed)

### Authentication
All authenticated endpoints require a Bearer token:
```
Authorization: Bearer {your-jwt-token}
```

### Getting Started
1. Use the `/api/auth/login` endpoint to get a JWT token
2. Include the token in the `Authorization` header for all protected endpoints
3. Token expires after 60 minutes (default)

## Key Endpoints

### Public (No Auth Required)
- `GET /api/health` - Health check
- `POST /api/public/vin-inquiry` - Check vehicle by VIN
- `POST /api/public/service-request` - Create service request

### Authentication
- `POST /api/auth/login` - Login and get token
- `POST /api/auth/register` - Register new user (Admin only)

### Jobs
- `GET /api/jobs` - Get all jobs (Admin/Dispatcher)
- `GET /api/jobs/{id}` - Get job by ID
- `POST /api/jobs` - Create job (Admin/Dispatcher)
- `POST /api/jobs/{id}/assign` - Assign driver (Admin/Dispatcher)
- `PUT /api/jobs/{id}/status` - Update job status
- `GET /api/jobs/my-jobs` - Get my jobs (Driver)
- `POST /api/jobs/{id}/photos` - Upload photo (Driver, max 5)
- `GET /api/jobs/{id}/photos` - Get job photos

### Vehicles
- `GET /api/vehicles` - Get all vehicles
- `GET /api/vehicles/{id}` - Get vehicle by ID
- `GET /api/vehicles/vin/{vin}` - Get vehicle by VIN
- `POST /api/vehicles` - Create vehicle (Admin/Dispatcher)

### Users (Admin Only)
- `GET /api/users` - Get all users
- `GET /api/users/{id}` - Get user by ID
- `POST /api/users` - Create user
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Deactivate user

### Payments
- `POST /api/payments` - Process payment
- `GET /api/payments/job/{jobId}` - Get payment by job

### Reports (Admin Only)
- `GET /api/reports/financial` - Extended financial summary (revenue, refunds, net, methods, status counts)
- `GET /api/reports/export?type=csv` - Export financial summary as CSV

## Job Status Flow

```
Pending → Assigned → OnRoute → InProgress → ReadyToRelease → Completed
```

**Important Rules:**
- `ReadyToRelease` requires exactly 5 photos
- Only Admin/Dispatcher can assign drivers
- Drivers can update status from `Assigned` onwards

## Error Responses

All errors follow this format:
```json
{
  "error": "Error Type",
  "message": "Detailed error message"
}
```

Common HTTP Status Codes:
- `200` - Success
- `201` - Created
- `400` - Bad Request
- `401` - Unauthorized
- `403` - Forbidden
- `404` - Not Found
- `500` - Internal Server Error

## File Uploads

For photo uploads (`POST /api/jobs/{id}/photos`):
- Use `multipart/form-data`
- Field name: `file`
- Max file size: 5MB
- Accepted formats: JPG, PNG
- Maximum 5 photos per job

## Date Formats

- All dates in API responses: ISO 8601 format (UTC)
- Example: `2024-01-15T10:30:00Z`
- Query parameters: `YYYY-MM-DD` format

## Testing the API

1. **Using Swagger UI**: 
   - Run the API: `dotnet run`
   - Visit: `http://localhost:5155/swagger`
   - Test endpoints directly in the browser

2. **Using Postman**:
   - Import `swagger.json` file
   - Set base URL to your environment
   - Add Bearer token in Authorization tab

3. **Using cURL**:
   ```bash
   # Login
   curl -X POST http://localhost:5155/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email":"user@example.com","password":"Password123!"}'
   
   # Get Jobs (with token)
   curl -X GET http://localhost:5155/api/jobs \
     -H "Authorization: Bearer YOUR_TOKEN_HERE"
   ```

## Notes for Frontend Development

1. **Environment Variables**: Use different base URLs for dev/prod
2. **Token Storage**: Store JWT in localStorage or httpOnly cookie
3. **Token Refresh**: Implement token refresh before expiration
4. **Error Handling**: Handle all error responses consistently
5. **File Uploads**: Use FormData for multipart uploads
6. **CORS**: API is configured to allow requests from your Angular app

## Support

For questions or issues, contact the backend development team.

## Payment Workflow (2026-03 update)

### New Public Entry Endpoint
- `POST /api/orders` (AllowAnonymous)
  - Creates guest request mapped to Job + Payment records.
  - Supports `paymentDueMode`:
    - `PayNow`: creates Stripe payment intent and returns `clientSecret`, `paymentIntentId`, `publishableKey`.
    - `PayLater`: creates payment in pending state.

### Payment Lifecycle
- Supported statuses:
  - `Unpaid`
  - `Pending`
  - `PendingCash`
  - `UnderReview`
  - `CapturePending`
  - `Authorized`
  - `Paid`
  - `Failed`
  - `Cancelled`
  - `Refunded`
  - `PartiallyRefunded`

### Fraud Review Queue
- Risk scoring now runs during `POST /api/orders` intake.
- High-risk requests are set to `PaymentStatus = UnderReview` and blocked from payment progression.
- Review endpoints:
  - `GET /api/payments/fraud-review-queue` (Admin/Dispatcher)
  - `POST /api/payments/{id}/review-decision` with `decision = approve|reject` (Admin/SuperAdmin)

### Manual-Capture Pre-Authorization (Stripe)
- `PayNow` requests default to pre-authorization when enabled in system settings.
- PaymentIntent uses Stripe manual capture flow.
- Auth-hold operations:
  - `POST /api/payments/{id}/capture-authorization`
  - `POST /api/payments/{id}/release-authorization`
- Webhook behavior:
  - `payment_intent.amount_capturable_updated` => marks payment `Authorized`
  - `payment_intent.succeeded` => marks payment `Paid` (capture completed)
- **Important:** authorization success alone does **not** mark payment as paid.

### Cancellation Fee Rules
- Cancellation fee matrix is configurable in system settings:
  - `CancelFeeBeforeDispatchPercent`
  - `CancelFeeAfterDispatchPercent`
  - `CancelFeeAfterArrivalPercent`
- Job cancellation endpoint:
  - `POST /api/jobs/{id}/cancel-with-fee`
- Cancellation fee settlement:
  - captures from authorization hold when available;
  - otherwise records cancellation outcome and payment status.

### New Settings Fields
- `PreAuthorizationEnabled`
- `PreAuthorizationMinAmount`
- `PreAuthorizationMaxAmount`
- `FraudReviewScoreThreshold`
- `DuplicateRequestWindowMinutes`
- `CancelFeeBeforeDispatchPercent`
- `CancelFeeAfterDispatchPercent`
- `CancelFeeAfterArrivalPercent`

### Payment Operations
- `POST /api/payments/{id}/mark-cash-pending` (Admin/Dispatcher)
- `POST /api/payments/{id}/mark-cash-collected` (Admin/Dispatcher)
- `POST /api/payments/{id}/cancel` (Admin/Dispatcher)

### Completion Settlement Enforcement
- `PUT /api/jobs/{id}/status` rejects transition to `Completed` when payment is not settled.
- `POST /api/jobs/{id}/complete-with-override` (SuperAdmin/Admin) allows explicit override with required reason and audit note.

### Rollout Guidance
- Stage 1: enable lifecycle + review queue only (`PreAuthorizationEnabled=false`).
- Stage 2: enable pre-authorization for selected operators and monitor webhook transitions.
- Stage 3: enable cancellation-fee policy once dispatch teams are trained.
- Keep dashboards focused on `UnderReview`, `Authorized`, and `Cancelled` counts during rollout week.


