# Driver Assignment System - Quick Start Guide

This guide explains how to use the Smart Driver Assignment system for efficiently assigning drivers to shipments.

## ?? Overview

The Smart Driver Assignment System automatically recommends the best drivers for shipments based on:

- **?? Regional Proximity**: How close the driver's region/city is to the shipment location
- **? Availability**: Driver status and current workload
- **? Rating**: Customer satisfaction scores
- **?? Experience**: Number of completed deliveries
- **?? Working Hours**: Driver's availability schedule
- **? Verification Status**: Licensed and verified drivers

## ?? Quick Start

### 1. Get Driver Recommendations

```http
GET /api/shipments/{shipmentId}/driver-recommendations?priority=Balanced
Authorization: Bearer {token}
```

**Response:**
```json
[
  {
    "driver": {
      "id": "driver-guid",
      "fullName": "John Driver",
      "email": "john@example.com",
      "phone": "+1234567890"
    },
    "distance": 5,
    "rating": 4.8,
    "activeShipments": 1,
    "completedShipments": 150,
    "score": 0.92,
    "recommendationReason": "Same region, Excellent rating"
  }
]
```

### 2. Auto-Assign Best Driver

```http
POST /api/shipments/{shipmentId}/smart-assign
Content-Type: application/json
Authorization: Bearer {token}

{
  "useAutoAssignment": true,
  "priority": "Balanced"
}
```

### 3. Manual Assignment with Recommendations

```http
POST /api/shipments/{shipmentId}/smart-assign
Content-Type: application/json
Authorization: Bearer {token}

{
  "useAutoAssignment": false,
  "maxRecommendations": 5,
  "priority": "Distance"
}
```

Then assign a specific driver:
```http
POST /api/shipments/{shipmentId}/smart-assign
Content-Type: application/json
Authorization: Bearer {token}

{
  "preferredDriverId": "selected-driver-guid",
  "priority": "Distance"
}
```

## ?? Assignment Priorities

Choose the right priority based on your business needs:

### ?? **Balanced** (Default - Recommended)
- **Use Case**: General shipments
- **Focus**: Overall optimization across all factors
- **Best For**: Day-to-day operations

### ?? **Distance** 
- **Use Case**: Urgent deliveries, cost optimization
- **Focus**: Closest available drivers
- **Best For**: Rush orders, minimizing delivery time

### ?? **Experience**
- **Use Case**: Valuable or complex shipments
- **Focus**: Most experienced drivers
- **Best For**: High-value items, special handling

### ? **Rating**
- **Use Case**: Customer satisfaction priority
- **Focus**: Highest-rated drivers
- **Best For**: VIP customers, service quality

### ?? **Availability**
- **Use Case**: Load balancing, high-volume periods
- **Focus**: Drivers with lowest workload
- **Best For**: Peak seasons, workload distribution

## ?? Driver Management

### Update Driver Location
```http
POST /api/drivers/location
Content-Type: application/json
Authorization: Bearer {token}

{
  "address": "Downtown Manhattan, NY"
}
```

### Update Driver Status
```http
PUT /api/drivers/status
Content-Type: application/json
Authorization: Bearer {token}

{
  "status": "Available"  // Available | Busy | OffDuty | OnBreak
}
```

### Update Driver Profile
```http
PUT /api/drivers/profile
Content-Type: application/json
Authorization: Bearer {token}

{
  "maxActiveShipments": 5,
  "vehicleType": "Van",
  "licenseNumber": "DL123456",
  "workStartTime": "08:00:00",
  "workEndTime": "18:00:00",
  "preferredRegion": "Manhattan"
}
```

## ?? Understanding Scores

The system provides a score (0-1) for each recommendation:

- **0.8 - 1.0** ?? **Excellent**: Perfect match, highly recommended
- **0.6 - 0.8** ?? **Good**: Good match, suitable for assignment
- **0.4 - 0.6** ?? **Fair**: Acceptable match, consider alternatives
- **0.2 - 0.4** ?? **Poor**: Not ideal, use only if necessary
- **0.0 - 0.2** ?? **Very Poor**: Avoid assignment

## ?? Best Practices

### For Administrators

1. **Regular Location Updates**: Ensure drivers update their addresses when changing locations
2. **Priority Selection**: Use appropriate priority for shipment type
3. **Monitor Capacity**: Keep driver workloads balanced
4. **Verify Status**: Check driver availability before peak periods

### For Drivers

1. **Keep Status Updated**: Set status accurately (Available, OnBreak, OffDuty)
2. **Location Updates**: Update your current address when changing locations
3. **Working Hours**: Configure your working schedule in the system
4. **Profile Completion**: Add vehicle type and license number

### Assignment Strategies

**?? Rush Deliveries:**
```json
{ "priority": "Distance", "useAutoAssignment": true }
```

**?? Premium Shipments:**
```json
{ "priority": "Rating", "useAutoAssignment": false }
```

**?? High Volume Days:**
```json
{ "priority": "Availability", "useAutoAssignment": true }
```

**?? Valuable Items:**
```json
{ "priority": "Experience", "useAutoAssignment": false }
```

## ?? Troubleshooting

### ? No Available Drivers Found

**Possible Causes:**
- All drivers are at maximum capacity
- No drivers in the same region or nearby areas
- All nearby drivers are OffDuty or OnBreak
- Shipment region information is missing

**Solutions:**
1. Check driver status and working hours
2. Verify shipment has origin city/region information
3. Consider drivers from nearby regions
4. Add more drivers to the system

### ? Low Assignment Scores

**Possible Causes:**
- Driver locations are outdated
- No nearby drivers available
- Drivers have low ratings
- High workload on available drivers

**Solutions:**
1. Ask drivers to update their location
2. Review driver performance and ratings
3. Balance workload distribution
4. Consider hiring more drivers in the area

### ? Assignment Failures

**Possible Causes:**
- Driver became unavailable after recommendation
- Network connectivity issues
- Driver reached maximum capacity
- Shipment already assigned

**Solutions:**
1. Refresh recommendations before assignment
2. Check driver current status
3. Try alternative recommended drivers
4. Verify shipment assignment status

## ?? API Endpoints Summary

| Endpoint | Method | Purpose | Access |
|----------|--------|---------|--------|
| `/api/shipments/{id}/driver-recommendations` | GET | Get driver recommendations | Admin |
| `/api/shipments/{id}/smart-assign` | POST | Smart driver assignment | Admin |
| `/api/shipments/available-drivers` | GET | List all drivers availability | Admin |
| `/api/drivers/{id}/location` | PUT | Update driver location | Driver/Admin |
| `/api/drivers/{id}/status` | PUT | Update driver status | Driver/Admin |
| `/api/drivers/{id}/profile` | PUT | Update driver profile | Driver/Admin |

## ?? Related Documentation

- [Frontend API Guide](FRONTEND_API_GUIDE.md) - Complete API documentation
- [Driver Assignment Guide](DRIVER_ASSIGNMENT_GUIDE.md) - Detailed technical documentation
- [API Examples](API_EXAMPLES.md) - Usage examples and test data

## ?? Support

For technical support or questions about the driver assignment system:

1. Check the troubleshooting section above
2. Review the detailed [Driver Assignment Guide](DRIVER_ASSIGNMENT_GUIDE.md)
3. Contact your system administrator
4. Refer to API documentation for specific endpoint details

---

**?? Tip**: Start with the "Balanced" priority for most shipments, then use specific priorities for special cases like rush deliveries or premium service.