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
- `GET /api/reports/financial` - Financial summary
- `GET /api/reports/export` - Export report (CSV/Excel/PDF)

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

