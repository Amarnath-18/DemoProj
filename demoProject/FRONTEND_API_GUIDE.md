# Frontend API Guide

This guide provides all the API endpoints with request and response schemas for the Logistic Shipment Tracker system.

## Table of Contents
1. [Base Configuration](#base-configuration)
2. [Authentication APIs](#authentication-apis)
3. [User Management APIs](#user-management-apis)
4. [Shipment Management APIs](#shipment-management-apis)
5. [Reports & Analytics APIs](#reports--analytics-apis)
6. [Error Handling](#error-handling)

---

## Base Configuration

### API Base URL
```
Development: http://localhost:5000/api
Production: https://your-domain.com/api
```

### Required Headers
```javascript
{
  'Content-Type': 'application/json'
  // Authentication is handled automatically via HTTP-only cookies
}
```

---

## Authentication APIs

### 1. Register User
**Endpoint:** `POST /api/auth/register`
**Access:** Public
**Description:** Register a new user account

#### Request Body
```json
{
  "fullName": "string (required, max 100 chars)",
  "email": "string (required, valid email)",
  "password": "string (required, min 6 chars)",
  "phone": "string (optional, valid phone)",
  "role": "Admin | Driver | Customer (required)"
}
```

#### Response (200 OK)
```json
{
  "user": {
    "id": 1,
    "fullName": "John Doe",
    "email": "john.doe@example.com",
    "phone": "+1234567890",
    "role": "Customer",
    "createdAt": "2024-01-01T00:00:00Z"
  }
}
```

#### Error Response (400 Bad Request)
```json
{
  "message": "User with this email already exists"
}
```

### 2. Login User
**Endpoint:** `POST /api/auth/login`
**Access:** Public
**Description:** Authenticate user and get authentication cookie

#### Request Body
```json
{
  "email": "string (required, valid email)",
  "password": "string (required)"
}
```

#### Response (200 OK)
```json
{
  "user": {
    "id": 1,
    "fullName": "John Doe",
    "email": "john.doe@example.com",
    "phone": "+1234567890",
    "role": "Customer",
    "createdAt": "2024-01-01T00:00:00Z"
  }
}
```

#### Error Response (400 Bad Request)
```json
{
  "message": "Invalid credentials"
}
```

### 3. Logout User
**Endpoint:** `POST /api/auth/logout`
**Access:** Public
**Description:** Clear authentication cookie

#### Request Body
None

#### Response (200 OK)
```json
{
  "message": "Logged out successfully"
}
```

### 4. Get Current User
**Endpoint:** `GET /api/auth/me`
**Access:** Authenticated
**Description:** Get current authenticated user information

#### Request Body
None

#### Response (200 OK)
```json
{
  "id": 1,
  "fullName": "John Doe",
  "email": "john.doe@example.com",
  "phone": "+1234567890",
  "role": "Customer",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

#### Error Response (404 Not Found)
```json
{
  "message": "User not found"
}
```

---

## User Management APIs

### 1. Get All Users
**Endpoint:** `GET /api/users`
**Access:** Admin only
**Description:** Get list of all users with optional role filtering

#### Query Parameters
```
?role=Admin|Driver|Customer (optional)
```

#### Response (200 OK)
```json
[
  {
    "id": 1,
    "fullName": "John Doe",
    "email": "john.doe@example.com",
    "phone": "+1234567890",
    "role": "Customer",
    "createdAt": "2024-01-01T00:00:00Z"
  }
]
```

### 2. Get User by ID
**Endpoint:** `GET /api/users/{id}`
**Access:** User (own profile) or Admin
**Description:** Get specific user details

#### Response (200 OK)
```json
{
  "id": 1,
  "fullName": "John Doe",
  "email": "john.doe@example.com",
  "phone": "+1234567890",
  "role": "Customer",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

### 3. Update User
**Endpoint:** `PUT /api/users/{id}`
**Access:** User (own profile) or Admin
**Description:** Update user profile

#### Request Body
```json
{
  "fullName": "string (optional)",
  "email": "string (optional, valid email)",
  "phone": "string (optional)"
}
```

#### Response (204 No Content)

### 4. Delete User
**Endpoint:** `DELETE /api/users/{id}`
**Access:** Admin only
**Description:** Delete user account

#### Response (204 No Content)

#### Error Response (400 Bad Request)
```json
{
  "message": "Cannot delete admin users"
}
```

### 5. Get All Drivers
**Endpoint:** `GET /api/users/drivers`
**Access:** Admin only
**Description:** Get list of all drivers

#### Response (200 OK)
```json
[
  {
    "id": 2,
    "fullName": "Mike Driver",
    "email": "mike.driver@logistictracker.com",
    "phone": "+1234567892",
    "role": "Driver",
    "createdAt": "2024-01-01T00:00:00Z"
  }
]
```

---

## Shipment Management APIs

### 1. Get All Shipments
**Endpoint:** `GET /api/shipments`
**Access:** Authenticated (filtered by role)
**Description:** Get shipments based on user role (Admin: all, Driver: assigned shipments, Customer: own shipments)

#### Response (200 OK)
```json
[
  {
    "id": 1,
    "trackingNumber": "LST123456",
    "sender": {
      "id": 1,
      "fullName": "John Doe",
      "email": "john.doe@example.com",
      "phone": "+1234567890",
      "role": "Customer",
      "createdAt": "2024-01-01T00:00:00Z"
    },
    "receiverName": "Jane Smith",
    "receiverEmail": "jane.smith@example.com",
    "receiverPhone": "+1234567891",
    "originAddress": "123 Main St, New York, NY 10001",
    "destinationAddress": "456 Oak Ave, Los Angeles, CA 90001",
    "originLatitude": 40.7128,
    "originLongitude": -74.0060,
    "destinationLatitude": 34.0522,
    "destinationLongitude": -118.2437,
    "status": "Created | PickedUp | InTransit | Delivered | Cancelled",
    "assignedDriver": {
      "id": 2,
      "fullName": "Mike Driver",
      "email": "mike.driver@logistictracker.com",
      "phone": "+1234567892",
      "role": "Driver",
      "createdAt": "2024-01-01T00:00:00Z"
    },
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-01-01T02:00:00Z",
    "trackingUpdates": [
      {
        "id": 1,
        "status": "PickedUp",
        "location": "New York Distribution Center",
        "latitude": 40.7128,
        "longitude": -74.0060,
        "remarks": "Package picked up from sender",
        "updatedBy": {
          "id": 2,
          "fullName": "Mike Driver",
          "email": "mike.driver@logistictracker.com",
          "phone": "+1234567892",
          "role": "Driver",
          "createdAt": "2024-01-01T00:00:00Z"
        },
        "timestamp": "2024-01-01T01:00:00Z"
      }
    ]
  }
]
```

### 2. Get Shipment by ID
**Endpoint:** `GET /api/shipments/{id}`
**Access:** Authenticated (role-based access)
**Description:** Get specific shipment details

#### Response (200 OK)
Same structure as single shipment in the array above.

### 3. Create New Shipment
**Endpoint:** `POST /api/shipments`
**Access:** Customer or Admin
**Description:** Create a new shipment

#### Request Body
```json
{
  "receiverName": "string (required, max 100 chars)",
  "receiverEmail": "string (required, valid email)",
  "receiverPhone": "string (optional)",
  "originAddress": "string (required)",
  "destinationAddress": "string (required)",
  "originLatitude": "number (optional)",
  "originLongitude": "number (optional)",
  "destinationLatitude": "number (optional)",
  "destinationLongitude": "number (optional)"
}
```

#### Response (201 Created)
Returns the created shipment object (same structure as GET response).

### 4. Track Shipment (Public)
**Endpoint:** `GET /api/shipments/track/{trackingNumber}`
**Access:** Public
**Description:** Track shipment by tracking number

#### Response (200 OK)
Same structure as shipment object above.

#### Error Response (404 Not Found)
```json
{
  "message": "Shipment not found"
}
```

### 5. Assign Driver to Shipment
**Endpoint:** `PUT /api/shipments/{id}/assign-driver`
**Access:** Admin only
**Description:** Assign a driver to a shipment

#### Request Body
```json
{
  "driverId": "number (required)"
}
```

#### Response (204 No Content)

#### Error Response (400 Bad Request)
```json
{
  "message": "Invalid driver"
}
```

### 6. Update Shipment Status
**Endpoint:** `PUT /api/shipments/{id}/status`
**Access:** Driver (assigned to shipment)
**Description:** Update shipment status with location

#### Request Body
```json
{
  "status": "Created | PickedUp | InTransit | Delivered | Cancelled (required)",
  "location": "string (optional)",
  "latitude": "number (optional)",
  "longitude": "number (optional)",
  "remarks": "string (optional)"
}
```

#### Response (204 No Content)

#### Error Response (403 Forbidden)
```json
{
  "message": "Forbidden"
}
```

---

## Reports & Analytics APIs

### 1. Get Dashboard Analytics
**Endpoint:** `GET /api/reports/analytics`
**Access:** Admin only
**Description:** Get dashboard statistics

#### Response (200 OK)
```json
{
  "totalShipments": 150,
  "activeShipments": 25,
  "deliveredShipments": 120,
  "pendingShipments": 5,
  "totalDrivers": 10,
  "totalCustomers": 50,
  "shipmentsByStatus": [
    {
      "status": "Delivered",
      "count": 120
    },
    {
      "status": "InTransit",
      "count": 20
    },
    {
      "status": "PickedUp",
      "count": 5
    },
    {
      "status": "Created",
      "count": 5
    }
  ],
  "monthlyStats": [
    {
      "month": "2024-01",
      "count": 50
    },
    {
      "month": "2024-02",
      "count": 45
    },
    {
      "month": "2024-03",
      "count": 55
    }
  ]
}
```

### 2. Generate Report
**Endpoint:** `POST /api/reports/generate`
**Access:** Admin only
**Description:** Generate PDF report

#### Request Body
```json
{
  "reportType": "Daily | Weekly | Monthly | Custom (required)",
  "startDate": "2024-01-01 (required)",
  "endDate": "2024-01-07 (required)"
}
```

#### Response (200 OK)
```json
{
  "id": 1,
  "reportType": "Weekly",
  "startDate": "2024-01-01",
  "endDate": "2024-01-07",
  "filePath": "/path/to/report.pdf",
  "generatedAt": "2024-01-01T00:00:00Z",
  "generatedBy": {
    "id": 1,
    "fullName": "Admin User",
    "email": "admin@logistictracker.com",
    "phone": "+1234567890",
    "role": "Admin",
    "createdAt": "2024-01-01T00:00:00Z"
  }
}
```

### 3. Download Report
**Endpoint:** `GET /api/reports/{id}/download`
**Access:** Admin only
**Description:** Download generated PDF report

#### Response (200 OK)
Returns PDF file as binary data.

### 4. Get All Reports
**Endpoint:** `GET /api/reports`
**Access:** Admin only
**Description:** Get list of generated reports

#### Response (200 OK)
```json
[
  {
    "id": 1,
    "reportType": "Weekly",
    "startDate": "2024-01-01",
    "endDate": "2024-01-07",
    "filePath": "/path/to/report.pdf",
    "generatedAt": "2024-01-01T00:00:00Z",
    "generatedBy": {
      "id": 1,
      "fullName": "Admin User",
      "email": "admin@logistictracker.com",
      "phone": "+1234567890",
      "role": "Admin",
      "createdAt": "2024-01-01T00:00:00Z"
    }
  }
]
```

---

## Error Handling

### Common HTTP Status Codes

#### 400 Bad Request
```json
{
  "message": "Validation error message"
}
```

#### 401 Unauthorized
```json
{
  "message": "Unauthorized"
}
```

#### 403 Forbidden
```json
{
  "message": "Forbidden"
}
```

#### 404 Not Found
```json
{
  "message": "Resource not found"
}
```

#### 500 Internal Server Error
```json
{
  "message": "Internal server error"
}
```

---

## Notes

- All authenticated endpoints use HTTP-only cookies for authentication
- Cookie-based authentication is automatically handled by the browser
- No need to manually handle JWT tokens in local storage
- The API uses role-based access control (Admin, Driver, Customer)
- All timestamps are in UTC format
- File uploads and downloads are handled as binary data
- Tracking numbers are auto-generated with format: LST{6-digit-number}