# Create Job Business Logic Documentation

## Overview
The `POST /api/jobs` endpoint creates a job with intelligent handling of vehicle and client data. It supports:
- Using existing vehicle/client IDs
- Creating new vehicles/clients on the fly
- Automatic deduplication by email (client) and VIN (vehicle)

## Request Structure

```typescript
interface CreateJobRequest {
  // Vehicle: Provide EITHER vehicleId OR vehicle (not both, not neither)
  vehicleId?: number;        // Use existing vehicle
  vehicle?: VehicleData;     // Create new vehicle
  
  // Client: Provide EITHER clientId OR client (not both, not neither)
  clientId?: string;         // Use existing client
  client?: ClientData;       // Create new client
  
  // Job data (required)
  cost: number;              // Required: Job cost (0.01 to 999999.99)
  notes?: string;            // Optional: Job notes
  pickupLocation?: string;   // Optional: Pickup location
  dropoffLocation?: string;  // Optional: Dropoff location
  serviceType?: string;      // Optional: Service type
}

interface VehicleData {
  vin: string;      // Required: Vehicle Identification Number (must be unique)
  make: string;     // Required: Vehicle make
  model: string;    // Required: Vehicle model
  year: number;     // Required: Year (1900-2100)
  color?: string;   // Optional: Vehicle color
}

interface ClientData {
  email: string;        // Required: Client email (must be valid email format)
  fullName: string;     // Required: Client full name (min 2 characters)
  phoneNumber?: string; // Optional: Phone number (must be valid format)
}
```

## Business Logic Flow

### Step 1: Client Resolution

**Option A: Using Existing Client (`clientId` provided)**
1. Look up client by `clientId`
2. If not found → **404 Not Found**
3. Verify client has "User" role
4. If wrong role → **400 Bad Request**: "The provided ClientId does not belong to a client (User role)"

**Option B: Creating/Using Client (`client` data provided)**
1. Check if client already exists by email
2. If exists:
   - Verify it has "User" role
   - If wrong role → **400 Bad Request**: "A user with this email exists but is not a client"
   - Use existing client (no new account created)
3. If not exists:
   - Create new client account with:
     - Email as username
     - Random generated password (12 characters, alphanumeric + special chars)
     - Role: "User"
     - IsActive: true
   - If creation fails → **400 Bad Request** with validation errors

### Step 2: Vehicle Resolution

**Option A: Using Existing Vehicle (`vehicleId` provided)**
1. Look up vehicle by `vehicleId`
2. If not found → **404 Not Found**
3. Verify vehicle belongs to the resolved client
4. If belongs to different client → **400 Bad Request**: "The vehicle does not belong to the specified client"

**Option B: Creating/Using Vehicle (`vehicle` data provided)**
1. Check if vehicle already exists by VIN
2. If exists:
   - Verify it belongs to the resolved client
   - If belongs to different client → **400 Bad Request**: "A vehicle with VIN {vin} already exists and belongs to a different client"
   - Use existing vehicle
3. If not exists:
   - Create new vehicle with:
     - VIN, Make, Model, Year, Color
     - OwnerId: Set to resolved client's ID
   - Save to database

### Step 3: Job Creation
1. Create job with:
   - VehicleId: The resolved vehicle's ID
   - Cost: From request
   - Notes: From request (optional)
   - Status: "Pending" (default)
   - CreatedAt: Current UTC timestamp
2. Save job to database
3. Return JobDto with all related data

## Validation Rules

### Required Fields
- `cost` (must be between 0.01 and 999999.99)
- Either `vehicleId` OR `vehicle` (not both, not neither)
- Either `clientId` OR `client` (not both, not neither)

### Client Data Validation
- `email`: Required, valid email format
- `fullName`: Required, minimum 2 characters
- `phoneNumber`: Optional, must be valid phone format if provided

### Vehicle Data Validation
- `vin`: Required, must be unique across all vehicles
- `make`: Required
- `model`: Required
- `year`: Required, must be between 1900 and 2100
- `color`: Optional

## Error Responses

| Status Code | Scenario | Error Message |
|------------|----------|---------------|
| 400 | Missing vehicle/client data | "Either VehicleId or Vehicle data must be provided" / "Either ClientId or Client data must be provided" |
| 400 | ClientId wrong role | "The provided ClientId does not belong to a client (User role)" |
| 400 | Email exists but wrong role | "A user with this email exists but is not a client" |
| 400 | Vehicle belongs to different client | "The vehicle does not belong to the specified client" |
| 400 | VIN exists for different client | "A vehicle with VIN {vin} already exists and belongs to a different client" |
| 404 | ClientId not found | "Client with ID {clientId} was not found" |
| 404 | VehicleId not found | "Vehicle with ID {vehicleId} was not found" |
| 500 | Server error | "An error occurred while creating the job" |

## Success Response (201 Created)

Returns `JobDto` with:
- Job details (id, status, cost, notes, createdAt)
- Vehicle details (id, vin, make, model, year, color)
- Client details (clientId, clientName, clientEmail, clientPhoneNumber)
- Driver details (driverId, driverName - null initially)
- PhotoCount: 0

## Frontend Implementation Recommendations

### Form Structure
```typescript
// Recommended form structure
{
  // Client Section
  useExistingClient: boolean,
  clientId?: string,
  client?: {
    email: string,
    fullName: string,
    phoneNumber?: string
  },
  
  // Vehicle Section
  useExistingVehicle: boolean,
  vehicleId?: number,
  vehicle?: {
    vin: string,
    make: string,
    model: string,
    year: number,
    color?: string
  },
  
  // Job Section
  cost: number,
  notes?: string,
  pickupLocation?: string,
  dropoffLocation?: string,
  serviceType?: string
}
```

### User Experience Flow
1. **Client Selection:**
   - Option 1: Search/select existing client → set `clientId`
   - Option 2: Enter new client info → set `client` object

2. **Vehicle Selection:**
   - Option 1: Search/select existing vehicle → set `vehicleId` (must belong to selected client)
   - Option 2: Enter new vehicle info → set `vehicle` object

3. **Job Details:**
   - Enter cost (required)
   - Enter optional fields (notes, locations, service type)

4. **Submit:**
   - Send POST request to `/api/jobs`
   - Handle success (201) or errors (400/404/500)

### Important Notes for Frontend
1. **Client-Vehicle Relationship**: If using existing vehicle, ensure it belongs to the selected client
2. **Email Deduplication**: If a client email already exists, the system will reuse the existing client
3. **VIN Deduplication**: If a VIN already exists for the same client, the system will reuse the existing vehicle
4. **Auto-Generated Passwords**: New clients get a random password - consider a password reset flow
5. **Validation**: Validate required fields and formats before submission
6. **Error Handling**: Show specific error messages to help users correct input

## Example Request Scenarios

### Scenario 1: New Client, New Vehicle
```json
{
  "client": {
    "email": "john@example.com",
    "fullName": "John Doe",
    "phoneNumber": "+1234567890"
  },
  "vehicle": {
    "vin": "1HGBH41JXMN109186",
    "make": "Honda",
    "model": "Civic",
    "year": 2021,
    "color": "Blue"
  },
  "cost": 150.00,
  "notes": "Vehicle won't start"
}
```

### Scenario 2: Existing Client, New Vehicle
```json
{
  "clientId": "existing-client-id-123",
  "vehicle": {
    "vin": "1HGBH41JXMN109186",
    "make": "Honda",
    "model": "Civic",
    "year": 2021,
    "color": "Blue"
  },
  "cost": 150.00
}
```

### Scenario 3: Existing Client, Existing Vehicle
```json
{
  "clientId": "existing-client-id-123",
  "vehicleId": 5,
  "cost": 150.00,
  "notes": "Second towing request"
}
```

### Scenario 4: New Client (Email Exists) - Auto-Reuse
```json
{
  "client": {
    "email": "existing@example.com",  // This email already exists
    "fullName": "Jane Doe",
    "phoneNumber": "+1234567890"
  },
  "vehicle": {
    "vin": "1HGBH41JXMN109186",
    "make": "Honda",
    "model": "Civic",
    "year": 2021
  },
  "cost": 150.00
}
```
**Result**: System finds existing client by email and reuses it (no duplicate created)

## API Endpoint Details

**Endpoint**: `POST /api/jobs`

**Authentication**: Required (Bearer token)
- Roles: `Administrator` or `Dispatcher` only

**Content-Type**: `application/json`

**Base URL**:
- Development: `http://localhost:5155/api`
- Production: `http://66.179.188.32:8080/api`

## Complete Request/Response Examples

### Successful Request
```http
POST /api/jobs HTTP/1.1
Host: localhost:5155
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "client": {
    "email": "newclient@example.com",
    "fullName": "New Client",
    "phoneNumber": "+1234567890"
  },
  "vehicle": {
    "vin": "1HGBH41JXMN109186",
    "make": "Honda",
    "model": "Civic",
    "year": 2021,
    "color": "Blue"
  },
  "cost": 150.00,
  "notes": "Vehicle won't start",
  "pickupLocation": "123 Main St",
  "dropoffLocation": "456 Oak Ave",
  "serviceType": "Towing"
}
```

### Successful Response (201 Created)
```json
{
  "id": 1,
  "status": "Pending",
  "vehicleId": 1,
  "vehicle": {
    "id": 1,
    "vin": "1HGBH41JXMN109186",
    "make": "Honda",
    "model": "Civic",
    "year": 2021,
    "color": "Blue"
  },
  "clientId": "client-user-id-123",
  "clientName": "New Client",
  "clientEmail": "newclient@example.com",
  "clientPhoneNumber": "+1234567890",
  "driverId": null,
  "driverName": null,
  "cost": 150.00,
  "notes": "Vehicle won't start",
  "photoCount": 0,
  "createdAt": "2024-01-15T10:00:00Z",
  "completedAt": null
}
```

### Error Response Example (400 Bad Request)
```json
{
  "error": "Bad Request",
  "message": "Either VehicleId or Vehicle data must be provided."
}
```

## Additional Notes

1. **Database Relationships**:
   - Every vehicle MUST have an owner (client with User role)
   - Jobs are linked to vehicles, and vehicles are linked to clients
   - This creates a clear chain: Job → Vehicle → Client

2. **Data Integrity**:
   - VINs are unique across the entire system
   - Client emails are unique
   - Vehicle ownership is enforced (vehicle must belong to the client specified)

3. **Performance Considerations**:
   - The endpoint performs multiple database lookups
   - Consider caching client/vehicle lookups on the frontend if possible
   - The endpoint is transactional - all operations succeed or fail together

4. **Future Enhancements**:
   - Consider adding bulk job creation
   - Consider adding job templates
   - Consider adding client/vehicle search endpoints for better UX

