# Dispatcher Documentation

## Table of Contents
1. [Overview](#overview)
2. [Dispatcher Pages](#dispatcher-pages)
3. [Job Creation - Complete Data Requirements](#job-creation---complete-data-requirements)
4. [Job Creation Workflow](#job-creation-workflow)
5. [Driver Assignment](#driver-assignment)
6. [Payment Link Management](#payment-link-management)
7. [Job Status Management](#job-status-management)
8. [Best Practices](#best-practices)

---

## Overview

Dispatchers are responsible for managing towing and roadside assistance jobs from creation to completion. They coordinate between customers, drivers, and the system to ensure efficient service delivery.

### Key Responsibilities
- Create and manage service jobs
- Assign drivers to jobs
- Track job progress and status
- Create payment links for customers
- Manage vehicle information
- Monitor active jobs and driver availability

---

## Dispatcher Pages

> **Note:** Dispatchers have dedicated routes under `/dispatcher` for better clarity and separation from admin functions. This provides a cleaner URL structure and better user experience.

### 1. **Dashboard** (`/dispatcher`)
- Overview of active jobs
- Today's statistics (pending, in-progress, completed)
- Quick actions and notifications
- Recent activity feed

### 2. **Jobs Management** (`/dispatcher/jobs`)
- View all jobs with filters
- Create new jobs
- Assign drivers
- Update job status
- View job details
- Search and filter functionality

### 3. **Vehicles** (`/dispatcher/vehicles`)
- View all vehicles in system
- Create new vehicle records
- Search vehicles by VIN
- View vehicle details

### 4. **Payments** (`/dispatcher/payments`)
- View all payment links
- Create payment links for jobs
- Manage payment link status
- Track payment completion

---

## Job Creation - Complete Data Requirements

Based on industry standards from leading towing dispatch applications (TowBook, FleetComplete, ClearPathGPS, etc.), the following information is required when creating a job:

### **1. Customer Information** ⭐
*(Note: Currently handled via vehicle association, but should be collected)*

- **Full Name** (Required)
  - Customer's complete name
  - Example: "John Smith"
  
- **Phone Number** (Required)
  - Primary contact number
  - Format: +1234567890 or (123) 456-7890
  - Used for driver communication and updates
  
- **Email Address** (Optional but recommended)
  - For sending confirmations and receipts
  - Example: "customer@email.com"
  
- **Alternate Contact** (Optional)
  - Secondary phone number
  - Contact person if customer is not available

### **2. Vehicle Information** ⭐⭐⭐ (CRITICAL)

#### **Required Vehicle Data:**
- **VIN (Vehicle Identification Number)** (Required)
  - 17-character unique identifier
  - Example: "1HGBH41JXMN109186"
  - Used to check if vehicle exists in system
  
- **Make** (Required)
  - Vehicle manufacturer
  - Examples: "Honda", "Ford", "Toyota", "Chevrolet"
  
- **Model** (Required)
  - Specific vehicle model
  - Examples: "Civic", "F-150", "Camry", "Silverado"
  
- **Year** (Required)
  - Manufacturing year
  - Format: YYYY (e.g., 2021, 2020, 2019)
  
- **Color** (Required)
  - Vehicle exterior color
  - Examples: "Blue", "Black", "White", "Red", "Silver"
  - Helps driver identify vehicle at location

#### **Additional Vehicle Details:**
- **License Plate Number** (Highly Recommended)
  - State/province and plate number
  - Example: "ABC-1234" or "CA-12345"
  - Critical for vehicle identification
  
- **Vehicle Type** (Recommended)
  - Category of vehicle
  - Options: "Sedan", "SUV", "Truck", "Motorcycle", "RV", "Commercial"
  
- **Vehicle Condition** (Recommended)
  - Current state of vehicle
  - Options: "Running", "Not Running", "Locked", "Unlocked", "Damaged", "Undamaged"

### **3. Service Location Information** ⭐⭐⭐ (CRITICAL)

#### **Pickup Location** (Required)
- **Full Address** (Required)
  - Complete street address
  - Example: "123 Main Street, Los Angeles, CA 90001"
  
- **City** (Required)
  - City name
  - Example: "Los Angeles"
  
- **State** (Required)
  - State abbreviation or full name
  - Example: "CA" or "California"
  
- **ZIP Code** (Required)
  - Postal/ZIP code
  - Example: "90001"
  
- **Coordinates** (Recommended - Auto-populated if possible)
  - Latitude and Longitude
  - Used for GPS navigation and distance calculation
  - Example: Lat: 34.0522, Lng: -118.2437
  
- **Landmarks/Additional Directions** (Highly Recommended)
  - Nearby landmarks or specific instructions
  - Examples: 
    - "Next to the Shell gas station"
    - "In the parking lot behind the building"
    - "Red vehicle parked on the right side"
  - Helps driver locate vehicle quickly

#### **Destination Location** (Required for Towing Services)
- **Full Address** (Required if towing)
  - Complete destination address
  - Example: "456 Oak Avenue, Los Angeles, CA 90002"
  
- **City** (Required if towing)
  
- **State** (Required if towing)
  
- **ZIP Code** (Required if towing)
  
- **Coordinates** (Recommended)
  - Destination GPS coordinates
  
- **Special Instructions** (Optional)
  - Delivery instructions
  - Example: "Leave keys with receptionist"

### **4. Service Details** ⭐⭐

- **Service Type** (Required)
  - Type of service requested
  - Options:
    - "Towing" - Vehicle towing service
    - "Roadside Assistance" - General roadside help
    - "Jump Start" - Battery jump start
    - "Tire Change" - Flat tire replacement
    - "Lockout" - Locked out of vehicle
    - "Fuel Delivery" - Out of gas service
    - "Winch" - Vehicle recovery
    - "Recovery" - Vehicle recovery from difficult location
    - "Other" - Other services
  
- **Service Description** (Required)
  - Detailed description of the issue
  - Examples:
    - "Vehicle won't start, battery appears dead"
    - "Flat tire on driver's side rear"
    - "Keys locked inside vehicle"
    - "Vehicle broke down on highway, needs towing"
  
- **Priority Level** (Required)
  - Urgency of the service
  - Options:
    - "Emergency" - Immediate response needed (accidents, highway breakdowns)
    - "High" - Urgent but not life-threatening
    - "Medium" - Standard priority
    - "Low" - Non-urgent, can be scheduled
  
- **Scheduled Date/Time** (Optional)
  - Preferred service time
  - Format: Date and time
  - If not specified, treated as immediate/ASAP

### **5. Cost and Payment Information** ⭐⭐

- **Service Cost** (Required)
  - Total cost for the service
  - Format: Decimal number (e.g., 150.00)
  - Should include:
    - Base service fee
    - Distance charges (if applicable)
    - Additional fees (after-hours, weekend, etc.)
  
- **Payment Method** (Recommended)
  - How customer will pay
  - Options:
    - "Cash"
    - "Credit Card"
    - "Debit Card"
    - "Insurance" (if covered)
    - "Payment Link" (online payment)
  
- **Payment Status** (Auto-set)
  - "Pending" - Not yet paid
  - "Paid" - Payment received
  - "Payment Link Sent" - Link generated, awaiting payment

### **6. Additional Information** ⭐

- **Special Instructions/Notes** (Optional but Recommended)
  - Any additional information for driver
  - Examples:
    - "Customer will be waiting at location"
    - "Vehicle is in parking garage, level 3"
    - "Customer has AAA membership"
    - "Call customer 10 minutes before arrival"
    - "Vehicle has special equipment, handle with care"
  
- **Insurance Information** (Optional)
  - Insurance company name
  - Policy number
  - Claim number (if applicable)
  - Insurance contact information
  
- **Reference Number** (Optional)
  - External reference (e.g., insurance claim #, previous job #)
  
- **Customer Notes** (Optional)
  - Any special requests or information from customer
  - Example: "Customer prefers morning service"

### **7. Job Metadata** (Auto-generated)

- **Job ID** - Auto-generated unique identifier
- **Created Date/Time** - When job was created
- **Created By** - Dispatcher who created the job
- **Status** - Initial status: "Pending"
- **Assigned Driver** - Initially empty, assigned later

---

## Job Creation Workflow

### **Step-by-Step Process:**

1. **Access Jobs Page**
   - Navigate to `/dispatcher/jobs`
   - Click "Create New Job" button

2. **Vehicle Information**
   - **Option A: Vehicle Exists**
     - Search for vehicle by VIN
     - Select existing vehicle from results
   - **Option B: New Vehicle**
     - Click "Create New Vehicle"
     - Enter VIN, Make, Model, Year, Color
     - Save vehicle record
     - Vehicle ID is auto-generated

3. **Customer Information**
   - If customer exists in system, link to vehicle
   - If new customer, collect:
     - Full Name
     - Phone Number
     - Email (optional)

4. **Service Location**
   - Enter pickup address (full address, city, state, ZIP)
   - Add landmarks/directions
   - If towing: Enter destination address
   - System auto-calculates coordinates (if geocoding enabled)

5. **Service Details**
   - Select service type from dropdown
   - Enter detailed description
   - Set priority level
   - Set scheduled time (if not immediate)

6. **Cost Calculation**
   - Enter service cost
   - System may auto-calculate based on:
     - Service type
     - Distance (pickup to destination)
     - Time of day (after-hours fees)
     - Day of week (weekend fees)

7. **Additional Notes**
   - Add special instructions
   - Add insurance information (if applicable)
   - Add any other relevant notes

8. **Review and Submit**
   - Review all entered information
   - Verify accuracy
   - Click "Create Job"
   - Job is created with status "Pending"

9. **Post-Creation**
   - Job appears in jobs list
   - Status: "Pending"
   - Ready for driver assignment

---

## Driver Assignment

### **When to Assign:**
- After job is created and verified
- When driver is available
- Based on driver location and job location proximity

### **Assignment Process:**

1. **View Pending Jobs**
   - Go to Jobs page
   - Filter by status: "Pending"
   - Select job to assign

2. **Select Driver**
   - View available drivers
   - Consider:
     - Driver's current location
     - Driver's current job load
     - Driver's vehicle type/capacity
     - Driver's experience/specialization
   - Select appropriate driver

3. **Assign Driver**
   - Click "Assign Driver" on job
   - Select driver from dropdown
   - Confirm assignment
   - Job status changes to "Assigned"

4. **Driver Notification**
   - Driver receives notification (if system supports)
   - Driver can view job details
   - Driver can accept/reject (if system supports)

---

## Payment Link Management

### **Creating Payment Links:**

1. **Access Payment Link Creation**
   - From Jobs page: Click "Create Payment Link" on completed job
   - From Payments page: Click "Create New Link" → Select job

2. **Select Job**
   - Choose job from dropdown
   - Job must have a cost amount
   - View job details (amount, customer, service type)

3. **Set Expiration** (Optional)
   - Default: 7 days
   - Can set custom expiration (1-30 days)
   - Link expires after set period

4. **Generate Link**
   - Click "Generate Payment Link"
   - System creates unique token
   - Full URL is generated: `https://yourdomain.com/pay/{token}`

5. **Share Link**
   - Copy link to clipboard
   - Send via:
     - Email to customer
     - SMS/Text message
     - Manual sharing
   - Link can be shared multiple times

### **Payment Link Features:**
- Unique token per link
- Tied to specific job
- Shows job amount
- Secure payment processing
- Expiration date tracking
- Can be deactivated if needed

### **Public Payment Page:**
- Customer opens link (no login required)
- Views job details and amount
- Enters payment information
- Processes payment securely
- Receives confirmation

---

## Job Status Management

### **Job Status Flow:**

```
Pending → Assigned → OnRoute → InProgress → ReadyToRelease → Completed
```

### **Status Descriptions:**

1. **Pending** (Initial)
   - Job created, waiting for driver assignment
   - Dispatcher action: Assign driver

2. **Assigned**
   - Driver assigned to job
   - Driver notified
   - Dispatcher action: Monitor progress

3. **OnRoute**
   - Driver en route to pickup location
   - Dispatcher action: Track driver location (if available)

4. **InProgress**
   - Driver at location, performing service
   - Dispatcher action: Monitor, assist if needed

5. **ReadyToRelease**
   - Service complete, vehicle ready
   - Waiting for customer pickup/approval
   - Dispatcher action: Confirm with customer

6. **Completed**
   - Job fully completed
   - Payment processed (if applicable)
   - Dispatcher action: Create payment link if needed

### **Status Update Permissions:**
- Dispatcher can update status manually if needed
- Driver typically updates status via mobile app
- System may auto-update based on driver actions

---

## Best Practices

### **Job Creation:**
1. **Always verify customer information**
   - Confirm phone number
   - Verify address accuracy
   - Double-check vehicle details

2. **Be specific with locations**
   - Use complete addresses
   - Add landmarks and directions
   - Include any access codes or gate information

3. **Set appropriate priority**
   - Use "Emergency" only for true emergencies
   - Consider customer needs and driver availability

4. **Document everything**
   - Add notes for special circumstances
   - Record customer preferences
   - Note any issues or concerns

### **Driver Assignment:**
1. **Consider proximity**
   - Assign closest available driver
   - Reduce response time

2. **Match driver to job**
   - Consider vehicle type requirements
   - Match driver experience to job complexity

3. **Balance workload**
   - Distribute jobs evenly
   - Don't overload single driver

### **Communication:**
1. **Keep customers informed**
   - Provide ETAs when possible
   - Update on delays
   - Confirm completion

2. **Coordinate with drivers**
   - Respond to driver questions quickly
   - Provide additional information as needed

### **Payment Links:**
1. **Create links promptly**
   - Generate after job completion
   - Send to customer immediately

2. **Follow up on payments**
   - Monitor payment status
   - Remind customers if payment pending

3. **Track expiration**
   - Monitor expiring links
   - Regenerate if needed

---

## Quick Reference Checklist

### **Creating a Job - Required Information:**

- [ ] Vehicle VIN
- [ ] Vehicle Make
- [ ] Vehicle Model
- [ ] Vehicle Year
- [ ] Vehicle Color
- [ ] Customer Name
- [ ] Customer Phone Number
- [ ] Pickup Address (Full)
- [ ] Pickup City
- [ ] Pickup State
- [ ] Pickup ZIP Code
- [ ] Destination Address (if towing)
- [ ] Service Type
- [ ] Service Description
- [ ] Priority Level
- [ ] Service Cost
- [ ] Special Instructions/Notes (recommended)

### **Optional but Recommended:**
- [ ] License Plate Number
- [ ] Customer Email
- [ ] Landmarks/Directions
- [ ] GPS Coordinates
- [ ] Scheduled Time
- [ ] Insurance Information
- [ ] Reference Number

---

## API Endpoints Reference

### **Job Management:**
- `GET /jobs` - Get all jobs (with optional status filter)
- `POST /jobs` - Create new job
- `GET /jobs/{id}` - Get job details
- `POST /jobs/{id}/assign` - Assign driver to job
- `PUT /jobs/{id}/status` - Update job status

### **Vehicle Management:**
- `GET /vehicles` - Get all vehicles
- `POST /vehicles` - Create new vehicle
- `GET /vehicles/{id}` - Get vehicle by ID
- `GET /vehicles/vin/{vin}` - Get vehicle by VIN

### **Payment Links:**
- `POST /payments/links` - Create payment link
- `GET /payments/links/job/{jobId}` - Get payment links for job
- `POST /payments/links/{linkToken}/clear` - Deactivate payment link

---

## Route Structure

### **Recommended Implementation:**

Dispatchers should use dedicated routes under `/dispatcher` instead of sharing `/admin` routes. This provides:

- **Clearer URLs:** `/dispatcher/jobs` is more intuitive than `/admin/jobs` for dispatchers
- **Better UX:** Users see their role reflected in the URL
- **Easier Customization:** Dispatcher-specific features can be added without affecting admin routes
- **Better Security:** Clearer separation of concerns and permissions

### **Route Mapping:**

| Feature | Dispatcher Route | Admin Route |
|---------|-----------------|-------------|
| Dashboard | `/dispatcher` | `/admin` |
| Jobs | `/dispatcher/jobs` | `/admin/jobs` |
| Vehicles | `/dispatcher/vehicles` | `/admin/vehicles` |
| Payments | `/dispatcher/payments` | `/admin/payments` |
| Users | ❌ Not accessible | `/admin/users` |
| Reports | ❌ Not accessible | `/admin/reports` |
| Settings | ❌ Not accessible | `/admin/settings` |

### **Access Control:**

- Dispatchers can only access routes under `/dispatcher`
- Admins and SuperAdmins have access to `/admin` routes
- Role-based guards ensure proper access control
- Sidebar menu automatically filters based on user role

---

## Support and Resources

For technical support or questions:
- Contact system administrator
- Refer to API documentation (swagger.json)
- Check system notifications for updates

---

**Last Updated:** 2024
**Version:** 1.1

