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
  "originLatitude": 40.7128,
  "originLongitude": -74.0060,
  "destinationLatitude": 34.0522,
  "destinationLongitude": -118.2437
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
  "originLatitude": 40.7128,
  "originLongitude": -74.0060,
  "destinationLatitude": 34.0522,
  "destinationLongitude": -118.2437,
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
```

### Update Shipment Status (Driver)

```http
PUT /api/shipments/1/status
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "status": "InTransit",
  "location": "Dallas Transit Hub",
  "latitude": 32.7767,
  "longitude": -96.7970,
  "remarks": "Package in transit to destination"
}
```

### Assign Driver to Shipment (Admin)

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

### Example React Service

```javascript
// api.js
const API_BASE_URL = 'http://localhost:5000/api';

class ApiService {
  constructor() {
    this.token = localStorage.getItem('token');
  }

  async login(email, password) {
    const response = await fetch(`${API_BASE_URL}/auth/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ email, password }),
    });

    if (response.ok) {
      const data = await response.json();
      this.token = data.token;
      localStorage.setItem('token', data.token);
      return data;
    }
    throw new Error('Login failed');
  }

  async getShipments() {
    const response = await fetch(`${API_BASE_URL}/shipments`, {
      headers: {
        'Authorization': `Bearer ${this.token}`,
      },
    });

    if (response.ok) {
      return await response.json();
    }
    throw new Error('Failed to fetch shipments');
  }

  async trackShipment(trackingNumber) {
    const response = await fetch(`${API_BASE_URL}/shipments/track/${trackingNumber}`);
    
    if (response.ok) {
      return await response.json();
    }
    throw new Error('Shipment not found');
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