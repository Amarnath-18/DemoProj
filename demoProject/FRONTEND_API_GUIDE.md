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
Development: https://localhost:5000/api (MUST use HTTPS for cookies to work with React)
Production: https://your-domain.com/api
```

### Required Headers
```javascript
{
  'Content-Type': 'application/json'
  // Authentication is handled automatically via HTTP-only cookies
}
```

### **Important: Date Handling**
**All DateTime values must be sent in UTC ISO 8601 format**
- ? Correct: `"2024-01-01T00:00:00.000Z"`
- ? Incorrect: `"2024-01-01"`, `"01/01/2024"`, local time formats

```javascript
// Always use .toISOString() for dates
const dateValue = new Date("2024-01-01").toISOString();
```

### **CRITICAL: React Frontend Cookie Setup**

**For cookies to work with React, you MUST:**

1. **Use HTTPS for both backend and frontend**:
   ```bash
   # Backend runs on: https://localhost:5000
   # React must run on HTTPS too: https://localhost:3000
   ```

2. **Configure React for HTTPS** - Add to your React `.env` file:
   ```env
   HTTPS=true
   SSL_CRT_FILE=node_modules/webpack-dev-server/ssl/server.crt
   SSL_KEY_FILE=node_modules/webpack-dev-server/ssl/server.key
   ```

3. **Include credentials in ALL requests**:
   ```javascript
   // For fetch API
   fetch('https://localhost:5000/api/auth/login', {
     method: 'POST',
     credentials: 'include', // REQUIRED!
     headers: { 'Content-Type': 'application/json' },
     body: JSON.stringify({ email, password })
   });

   // For Axios (configure once globally)
   axios.defaults.withCredentials = true;
   axios.defaults.baseURL = 'https://localhost:5000/api';
   ```

4. **Verify CORS configuration** - Backend already configured for:
   - `https://localhost:3000` (Create React App)
   - `https://localhost:5173` (Vite)
   - Add your specific frontend URL if different

### React Authentication Hook Example
```javascript
// hooks/useAuth.js
import { useState, useEffect } from 'react';
import axios from 'axios';

// Configure axios globally
axios.defaults.withCredentials = true;
axios.defaults.baseURL = 'https://localhost:5000/api';

export const useAuth = () => {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  // Check if user is authenticated on app load
  useEffect(() => {
    checkAuth();
  }, []);

  const checkAuth = async () => {
    try {
      const response = await axios.get('/auth/me');
      setUser(response.data);
    } catch (error) {
      setUser(null);
    } finally {
      setLoading(false);
    }
  };

  const login = async (email, password) => {
    try {
      const response = await axios.post('/auth/login', { email, password });
      setUser(response.data.user);
      return { success: true, user: response.data.user };
    } catch (error) {
      return { 
        success: false, 
        message: error.response?.data?.message || 'Login failed' 
      };
    }
  };

  const logout = async () => {
    try {
      await axios.post('/auth/logout');
      setUser(null);
      return { success: true };
    } catch (error) {
      // Even if logout fails on server, clear local state
      setUser(null);
      return { success: false };
    }
  };

  return { user, loading, login, logout, checkAuth };
};
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
    "originCity": "New York",
    "originRegion": "NY", 
    "destinationCity": "Los Angeles",
    "destinationRegion": "CA",
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
  "originCity": "string (optional)",
  "originRegion": "string (optional)",
  "destinationCity": "string (optional)",
  "destinationRegion": "string (optional)"
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

### 6. Get Driver Recommendations for Shipment
**Endpoint:** `GET /api/shipments/{id}/driver-recommendations`
**Access:** Admin only
**Description:** Get intelligent driver recommendations for a shipment

#### Query Parameters
```
?priority=Distance|Experience|Rating|Availability|Balanced (optional, default: Balanced)
```

#### Response (200 OK)
```json
[
  {
    "driver": {
      "id": 2,
      "fullName": "Mike Driver",
      "email": "mike.driver@logistictracker.com",
      "phone": "+1234567892",
      "role": "Driver",
      "createdAt": "2024-01-01T00:00:00Z"
    },
    "driverDetails": {
      "status": "Available",
      "currentAddress": "Manhattan, NY",
      "maxActiveShipments": 5,
      "vehicleType": "Van",
      "licenseNumber": "DL123456",
      "isVerified": true,
      "lastActiveTime": "2024-01-01T08:00:00Z",
      "workStartTime": "08:00:00",
      "workEndTime": "18:00:00",
      "preferredRegion": "New York"
    },
    "distance": 2.5,
    "activeShipments": 1,
    "rating": 4.8,
    "completedShipments": 150,
    "lastLocationUpdate": "2024-01-01T08:30:00Z",
    "score": 0.92,
    "recommendationReason": "Very close distance, Excellent rating, Experienced",
    "recommendationFactors": [
      "Very close distance",
      "Excellent rating",
      "Highly experienced",
      "Verified driver"
    ]
  }
]
```

### 7. Smart Driver Assignment
**Endpoint:** `POST /api/shipments/{id}/smart-assign`
**Access:** Admin only
**Description:** Intelligently assign driver to shipment or get recommendations

#### Request Body
```json
{
  "preferredDriverId": "guid (optional, override automatic selection)",
  "useAutoAssignment": "boolean (default: true, auto-assign best driver)",
  "maxRecommendations": "number (default: 5, max recommendations to return)",
  "priority": "Distance | Experience | Rating | Availability | Balanced (default: Balanced)"
}
```

#### Response (200 OK)
If `useAutoAssignment` is true or `preferredDriverId` is specified:
```json
{
  "driver": {
    "id": 2,
    "fullName": "Mike Driver",
    "email": "mike.driver@logistictracker.com",
    "phone": "+1234567892",
    "role": "Driver",
    "createdAt": "2024-01-01T00:00:00Z"
  },
  "driverDetails": {
    "status": "Busy",
    "currentAddress": "Manhattan, NY",
    "maxActiveShipments": 5,
    "vehicleType": "Van",
    "licenseNumber": "DL123456",
    "isVerified": true,
    "lastActiveTime": "2024-01-01T08:00:00Z",
    "workStartTime": "08:00:00",
    "workEndTime": "18:00:00",
    "preferredRegion": "New York"
  },
  "distance": 2.5,
  "activeShipments": 2,
  "rating": 4.8,
  "completedShipments": 150,
  "lastLocationUpdate": "2024-01-01T08:30:00Z",
  "score": 0.92,
  "recommendationReason": "Very close distance, Excellent rating, Experienced",
  "recommendationFactors": [
    "Very close distance",
    "Excellent rating",
    "Highly experienced",
    "Verified driver"
  ]
}
```

If `useAutoAssignment` is false, returns array of recommendations (same as driver-recommendations endpoint).

### 8. Get Available Drivers
**Endpoint:** `GET /api/shipments/available-drivers`
**Access:** Admin only
**Description:** Get all drivers with their availability status

#### Response (200 OK)
```json
[
  {
    "driver": {
      "id": 2,
      "fullName": "Mike Driver",
      "email": "mike.driver@logistictracker.com",
      "phone": "+1234567892",
      "role": "Driver",
      "createdAt": "2024-01-01T00:00:00Z"
    },
    "driverDetails": {
      "status": "Available",
      "currentAddress": "Manhattan, NY",
      "maxActiveShipments": 5,
      "vehicleType": "Van",
      "licenseNumber": "DL123456",
      "isVerified": true,
      "lastActiveTime": "2024-01-01T08:00:00Z",
      "workStartTime": "08:00:00",
      "workEndTime": "18:00:00",
      "preferredRegion": "New York"
    },
    "activeShipments": 2,
    "isAvailable": true,
    "availabilityReason": "Available"
  }
]
```

### 9. Assign Driver to Shipment (Legacy)
**Endpoint:** `PUT /api/shipments/{id}/assign-driver`
**Access:** Admin only
**Description:** Manually assign a specific driver to a shipment (use smart-assign instead for better results)

#### Request Body
```json
{
  "driverId": "guid (required)"
}
```

#### Response (204 No Content)

#### Error Response (400 Bad Request)
```json
{
  "message": "Invalid driver"
}
```

### 10. Update Shipment Status
**Endpoint:** `PUT /api/shipments/{id}/status`
**Access:** Driver (assigned to shipment)
**Description:** Update shipment status with location

#### Request Body
```json
{
  "status": "Created | PickedUp | InTransit | Delivered | Cancelled (required)",
  "location": "string (optional)",
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

## Driver Management APIs

### 1. Update Driver Location
**Endpoint:** `POST /api/drivers/location`
**Access:** Driver (own profile only)
**Description:** Update driver's current location

#### Request Body
```json
{
  "address": "string (required)"
}
```

#### Response (204 No Content)

### 2. Update Driver Status
**Endpoint:** `PUT /api/drivers/status`
**Access:** Driver (own profile only)
**Description:** Update driver availability status

#### Request Body
```json
{
  "status": "Available | Busy | OffDuty | OnBreak (required)"
}
```

#### Response (204 No Content)

### 3. Update Driver Profile
**Endpoint:** `PUT /api/drivers/profile`
**Access:** Driver (own profile only)
**Description:** Update driver profile settings

#### Request Body
```json
{
  "maxActiveShipments": "number (1-5, default: 5)",
  "vehicleType": "string (optional, max 50 chars)",
  "licenseNumber": "string (optional, max 20 chars)",
  "workStartTime": "time (optional, format: HH:mm:ss)",
  "workEndTime": "time (optional, format: HH:mm:ss)",
  "preferredRegion": "string (optional, max 50 chars)"
}
```

#### Response (204 No Content)

### 4. Create Driver Profile
**Endpoint:** `POST /api/drivers/profile`
**Access:** Driver (own profile only)
**Description:** Create driver profile (required for new drivers)

#### Response (204 No Content)

### 5. Get Driver Availability
**Endpoint:** `GET /api/drivers/availability`
**Access:** Admin only
**Description:** Get all drivers with availability status

#### Response (200 OK)
```json
[
  {
    "driver": {
      "id": 2,
      "fullName": "Mike Driver",
      "email": "mike.driver@logistictracker.com",
      "phone": "+1234567892",
      "role": "Driver",
      "createdAt": "2024-01-01T00:00:00Z"
    },
    "driverDetails": {
      "status": "Available",
      "currentAddress": "Manhattan, NY",
      "maxActiveShipments": 5,
      "vehicleType": "Van",
      "licenseNumber": "DL123456",
      "isVerified": true,
      "lastActiveTime": "2024-01-01T08:00:00Z",
      "workStartTime": "08:00:00",
      "workEndTime": "18:00:00",
      "preferredRegion": "New York"
    },
    "activeShipments": 2,
    "isAvailable": true,
    "availabilityReason": "Available"
  }
]
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
  "startDate": "2024-01-01T00:00:00.000Z (required, ISO 8601 UTC format)",
  "endDate": "2024-01-07T23:59:59.999Z (required, ISO 8601 UTC format)"
}
```

**Important:** Always send dates in UTC ISO 8601 format. In JavaScript:
```javascript
// Correct way to send dates
const request = {
  reportType: "Weekly",
  startDate: new Date("2024-01-01").toISOString(), // "2024-01-01T00:00:00.000Z"
  endDate: new Date("2024-01-07").toISOString()    // "2024-01-07T00:00:00.000Z"
};
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