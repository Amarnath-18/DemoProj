# API Usage Examples

This document provides examples of how to use the Logistic Shipment Tracker API endpoints.

## Authentication

### Register a New User

```http
POST /api/auth/register
Content-Type: application/json

{
  "fullName": "John Doe",
  "email": "john.doe@example.com",
  "password": "SecurePassword123!",
  "phone": "+1234567890",
  "role": "Customer"
}
```

### Login

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "john.doe@example.com",
  "password": "SecurePassword123!"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
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

## Shipment Management

### Create a New Shipment (Customer/Admin)

```http
POST /api/shipments
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "receiverName": "Jane Smith",
  "receiverEmail": "jane.smith@example.com",
  "receiverPhone": "+1234567891",
  "originAddress": "123 Main St, New York, NY 10001",
  "destinationAddress": "456 Oak Ave, Los Angeles, CA 90001",
  "originCity": "New York",
  "originRegion": "NY",
  "destinationCity": "Los Angeles", 
  "destinationRegion": "CA"
}
```

### Track Shipment (Public)

```http
GET /api/shipments/track/LST123456
```

**Response:**
```json
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
  "status": "InTransit",
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
```

### Update Shipment Status (Driver)

```http
PUT /api/shipments/1/status
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "status": "InTransit",
  "location": "Dallas Transit Hub",
  "remarks": "Package in transit to destination"
}
```

### Smart Driver Assignment (Admin)

#### Get Driver Recommendations
```http
GET /api/shipments/1/driver-recommendations?priority=Balanced
Authorization: Bearer YOUR_JWT_TOKEN
```

#### Auto-Assign Best Driver
```http
POST /api/shipments/1/smart-assign
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "useAutoAssignment": true,
  "priority": "Balanced"
}
```

#### Manual Assignment with Specific Driver
```http
POST /api/shipments/1/smart-assign
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "preferredDriverId": "driver-guid-here",
  "priority": "Experience"
}
```

### Legacy Driver Assignment (Admin)

```http
PUT /api/shipments/1/assign-driver
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "driverId": 2
}
```

## User Management

### Get All Users (Admin)

```http
GET /api/users
Authorization: Bearer YOUR_JWT_TOKEN
```

### Get Drivers Only (Admin)

```http
GET /api/users/drivers
Authorization: Bearer YOUR_JWT_TOKEN
```

### Get Users by Role (Admin)

```http
GET /api/users?role=Driver
Authorization: Bearer YOUR_JWT_TOKEN
```

### Update User Profile

```http
PUT /api/users/1
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "fullName": "John Updated Doe",
  "phone": "+1234567999"
}
```

## Driver Management

### Update Driver Location (Driver)

```http
POST /api/drivers/location
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "address": "Downtown Manhattan, NY"
}
```

### Update Driver Status (Driver)

```http
PUT /api/drivers/status
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "status": "Available"
}
```

### Create Driver Profile (Driver)

```http
POST /api/drivers/profile
Authorization: Bearer YOUR_JWT_TOKEN
```

### Update Driver Profile (Driver)

```http
PUT /api/drivers/profile
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "maxActiveShipments": 5,
  "vehicleType": "Van",
  "licenseNumber": "DL123456",
  "workStartTime": "08:00:00",
  "workEndTime": "18:00:00",
  "preferredRegion": "Manhattan"
}
```

### Get Driver Availability (Admin)

```http
GET /api/drivers/availability
Authorization: Bearer YOUR_JWT_TOKEN
```

## Reports and Analytics

### Generate Report (Admin)

```http
POST /api/reports/generate
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "reportType": "Weekly",
  "startDate": "2024-01-01",
  "endDate": "2024-01-07"
}
```

### Download Report (Admin)

```http
GET /api/reports/1/download
Authorization: Bearer YOUR_JWT_TOKEN
```

### Get Dashboard Analytics (Admin)

```http
GET /api/reports/analytics
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response:**
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

## Sample Test Data

The application includes sample data for testing:

### Test Users:
- **Admin**: admin@logistictracker.com / Admin123!
- **Driver 1**: john.driver@logistictracker.com / Driver123!
- **Driver 2**: jane.driver@logistictracker.com / Driver123!
- **Customer 1**: alice.customer@example.com / Customer123!
- **Customer 2**: bob.customer@example.com / Customer123!

### Sample Tracking Numbers:
- LST123456 (Created)
- LST789012 (Picked Up)
- LST345678 (Delivered)

## Error Responses

### 400 Bad Request
```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Email": ["The Email field is required."]
  }
}
```

### 401 Unauthorized
```json
{
  "message": "Unauthorized"
}
```

### 403 Forbidden
```json
{
  "message": "Forbidden"
}
```

### 404 Not Found
```json
{
  "message": "Shipment not found"
}
```

## Using with Frontend (React)

### Example React Service (Cookie-Based Authentication)

```javascript
// api.js
const API_BASE_URL = 'https://localhost:5000/api'; // MUST use HTTPS for cookies

class ApiService {
  constructor() {
    // No need to store tokens - cookies handle authentication automatically
  }

  async login(email, password) {
    const response = await fetch(`${API_BASE_URL}/auth/login`, {
      method: 'POST',
      credentials: 'include', // REQUIRED for cookies
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ email, password }),
    });

    if (response.ok) {
      const data = await response.json();
      // Cookie is automatically set by browser
      return data;
    }
    throw new Error('Login failed');
  }

  async logout() {
    const response = await fetch(`${API_BASE_URL}/auth/logout`, {
      method: 'POST',
      credentials: 'include',
    });
    
    if (response.ok) {
      return await response.json();
    }
    throw new Error('Logout failed');
  }

  async getCurrentUser() {
    const response = await fetch(`${API_BASE_URL}/auth/me`, {
      credentials: 'include',
    });

    if (response.ok) {
      return await response.json();
    }
    return null; // Not authenticated
  }

  async getShipments() {
    const response = await fetch(`${API_BASE_URL}/shipments`, {
      credentials: 'include', // Send cookies
    });

    if (response.ok) {
      return await response.json();
    }
    if (response.status === 401) {
      throw new Error('Authentication required');
    }
    throw new Error('Failed to fetch shipments');
  }

  async createShipment(shipmentData) {
    const response = await fetch(`${API_BASE_URL}/shipments`, {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(shipmentData),
    });

    if (response.ok) {
      return await response.json();
    }
    throw new Error('Failed to create shipment');
  }

  async trackShipment(trackingNumber) {
    const response = await fetch(`${API_BASE_URL}/shipments/track/${trackingNumber}`);
    
    if (response.ok) {
      return await response.json();
    }
    throw new Error('Shipment not found');
  }

  async updateDriverLocation(address) {
    const response = await fetch(`${API_BASE_URL}/drivers/location`, {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ address }),
    });

    if (response.ok) {
      return true;
    }
    throw new Error('Failed to update location');
  }

  async updateShipmentStatus(shipmentId, statusData) {
    const response = await fetch(`${API_BASE_URL}/shipments/${shipmentId}/status`, {
      method: 'PUT',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(statusData),
    });

    if (response.ok) {
      return true;
    }
    throw new Error('Failed to update shipment status');
  }

  async getDriverRecommendations(shipmentId, priority = 'Balanced') {
    const response = await fetch(`${API_BASE_URL}/shipments/${shipmentId}/driver-recommendations?priority=${priority}`, {
      credentials: 'include',
    });

    if (response.ok) {
      return await response.json();
    }
    throw new Error('Failed to get driver recommendations');
  }

  async smartAssignDriver(shipmentId, assignmentData) {
    const response = await fetch(`${API_BASE_URL}/shipments/${shipmentId}/smart-assign`, {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(assignmentData),
    });

    if (response.ok) {
      return await response.json();
    }
    throw new Error('Failed to assign driver');
  }
}

export default new ApiService();
```

## Postman Collection

You can import the following endpoints into Postman for testing:

1. Set up environment variables:
   - `baseUrl`: http://localhost:5000/api
   - `token`: (will be set after login)

2. Create requests for each endpoint mentioned above
3. Use `{{baseUrl}}` and `{{token}}` variables in your requests

This API provides a complete backend solution for your Logistic Shipment Tracker application with all the features outlined in your requirements.