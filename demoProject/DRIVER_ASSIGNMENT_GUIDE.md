# Smart Driver Assignment System

This document explains how the intelligent driver assignment system works in the Logistic Shipment Tracker application.

## Table of Contents
1. [System Overview](#system-overview)
2. [Assignment Factors](#assignment-factors)
3. [Assignment Priorities](#assignment-priorities)
4. [Scoring Algorithm](#scoring-algorithm)
5. [API Usage Examples](#api-usage-examples)
6. [Frontend Integration](#frontend-integration)
7. [Best Practices](#best-practices)

---

## System Overview

The Smart Driver Assignment System automatically selects the most suitable driver for a shipment based on multiple factors including distance, availability, experience, rating, and working hours. The system provides both automatic assignment and manual selection with intelligent recommendations.

### Key Features

- **Multi-factor Analysis**: Considers distance, availability, experience, rating, and driver preferences
- **Flexible Prioritization**: Different assignment strategies (Distance, Experience, Rating, Availability, Balanced)
- **Real-time Availability**: Checks driver status, working hours, and current workload
- **Scoring System**: Provides transparent scoring for each recommendation
- **Fallback Options**: Multiple driver recommendations if automatic assignment fails

---

## Assignment Factors

### 1. Regional Proximity Factor
- Matches drivers based on preferred regions and current locations
- Uses city and region-based matching for assignment scoring
- Perfect matches get highest priority (same region)
- Good matches include same city or nearby areas
- Simplified location system without GPS coordinates

```csharp
// Region matching example
var regionScore = CalculateRegionMatch(shipment, driver);
// 0 = perfect match (same region)
// 5 = good match (same city)  
// 25 = acceptable match (nearby area)
```

### 2. Availability Factor (Workload Management)
- Checks driver status: `Available`, `Busy`, `OffDuty`, `OnBreak`
- Validates current active shipment count vs. maximum capacity
- Considers working hours (if configured)
- Available drivers with lower workload score higher

**Driver Status Types:**
- `Available`: Ready to accept new shipments
- `Busy`: Currently handling shipments but may have capacity
- `OffDuty`: Not working, cannot accept assignments
- `OnBreak`: Temporarily unavailable

### 3. Experience Factor (Completed Shipments)
- Based on total number of completed deliveries
- More experienced drivers handle complex or high-priority shipments better
- Experience score: `Math.Min(1.0, completedShipments / 100.0)`

### 4. Rating Factor (Customer Satisfaction)
- Average rating from previous deliveries (1-5 scale)
- Higher-rated drivers provide better service quality
- Rating score: `rating / 5.0`

### 5. Driver Profile Factors
- **Verification Status**: Verified drivers get 10% bonus
- **Recent Activity**: Active within 2 hours gets 5% bonus
- **Location Freshness**: Stale location (>1 hour) gets 20% penalty
- **Vehicle Type**: Can be matched to shipment requirements (future enhancement)
- **Preferred Region**: Drivers working in their preferred areas

---

## Assignment Priorities

The system supports different assignment strategies to meet various business needs:

### 1. Distance Priority
**Use Case**: Urgent deliveries, cost optimization
**Weights**: Distance (80%) + Availability (20%)
**Best For**: Time-critical shipments, minimizing travel costs

```json
{
  "priority": "Distance"
}
```

### 2. Experience Priority  
**Use Case**: Valuable or fragile items, complex deliveries
**Weights**: Experience (60%) + Rating (30%) + Availability (10%)
**Best For**: High-value shipments, customer VIP packages

```json
{
  "priority": "Experience"
}
```

### 3. Rating Priority
**Use Case**: Customer satisfaction focus, brand reputation
**Weights**: Rating (70%) + Experience (20%) + Availability (10%)
**Best For**: Customer retention, premium service level

```json
{
  "priority": "Rating"
}
```

### 4. Availability Priority
**Use Case**: High-volume periods, load balancing
**Weights**: Availability (60%) + Distance (40%)
**Best For**: Peak seasons, distributing workload evenly

```json
{
  "priority": "Availability"
}
```

### 5. Balanced Priority (Default)
**Use Case**: General shipments, optimal overall performance
**Weights**: Distance (30%) + Availability (25%) + Rating (25%) + Experience (20%)
**Best For**: Most shipments, balanced performance across all factors

```json
{
  "priority": "Balanced"
}
```

---

## Scoring Algorithm

### Base Score Calculation

Each driver receives a score between 0 and 1 based on the selected priority:

```csharp
// Example: Balanced Priority Scoring
double score = 
    GetDistanceScore(distance) * 0.3 +
    GetAvailabilityScore(activeShipments, maxCapacity) * 0.25 +
    GetRatingScore(rating) * 0.25 +
    GetExperienceScore(completedShipments) * 0.2;
```

### Score Modifiers

**Bonuses:**
- Verified Driver: +10% (`score *= 1.1`)
- Recent Activity (< 2 hours): +5% (`score *= 1.05`)

**Penalties:**
- Stale Location (> 1 hour): -20% (`score *= 0.8`)
- No Location Data: -40% (`score *= 0.6`)

### Final Score Range
- **0.8 - 1.0**: Excellent match
- **0.6 - 0.8**: Good match
- **0.4 - 0.6**: Fair match
- **0.2 - 0.4**: Poor match
- **0.0 - 0.2**: Not recommended

---

## API Usage Examples

### 1. Get Driver Recommendations

```javascript
// Get top 5 driver recommendations with distance priority
const response = await fetch('/api/shipments/123/driver-recommendations?priority=Distance', {
  method: 'GET',
  credentials: 'include'
});

const recommendations = await response.json();
console.log('Top recommended driver:', recommendations[0]);
```

### 2. Automatic Smart Assignment

```javascript
// Automatically assign best driver using balanced priority
const assignmentResponse = await fetch('/api/shipments/123/smart-assign', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  credentials: 'include',
  body: JSON.stringify({
    useAutoAssignment: true,
    priority: 'Balanced'
  })
});

const assignedDriver = await assignmentResponse.json();
console.log('Assigned driver:', assignedDriver.driver.fullName);
console.log('Assignment score:', assignedDriver.score);
```

### 3. Manual Selection with Recommendations

```javascript
// Get recommendations for manual selection
const recommendationsResponse = await fetch('/api/shipments/123/smart-assign', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  credentials: 'include',
  body: JSON.stringify({
    useAutoAssignment: false,
    maxRecommendations: 10,
    priority: 'Rating'
  })
});

const driverOptions = await recommendationsResponse.json();

// Admin selects a specific driver
const selectedDriverId = driverOptions[2].driver.id;
const assignmentResponse = await fetch('/api/shipments/123/smart-assign', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  credentials: 'include',
  body: JSON.stringify({
    preferredDriverId: selectedDriverId,
    priority: 'Rating'
  })
});
```

### 4. Check Driver Availability

```javascript
// Get all drivers with availability status
const driversResponse = await fetch('/api/shipments/available-drivers', {
  method: 'GET',
  credentials: 'include'
});

const drivers = await driversResponse.json();
drivers.forEach(driver => {
  console.log(`${driver.driver.fullName}: ${driver.availabilityReason}`);
});
```

---

## Frontend Integration

### React Component Example

```jsx
import React, { useState, useEffect } from 'react';
import axios from 'axios';

const SmartDriverAssignment = ({ shipmentId }) => {
  const [recommendations, setRecommendations] = useState([]);
  const [selectedPriority, setSelectedPriority] = useState('Balanced');
  const [loading, setLoading] = useState(false);
  const [assignedDriver, setAssignedDriver] = useState(null);

  // Get driver recommendations
  const fetchRecommendations = async () => {
    setLoading(true);
    try {
      const response = await axios.get(
        `/shipments/${shipmentId}/driver-recommendations`,
        { params: { priority: selectedPriority } }
      );
      setRecommendations(response.data);
    } catch (error) {
      console.error('Failed to fetch recommendations:', error);
    } finally {
      setLoading(false);
    }
  };

  // Auto-assign best driver
  const autoAssignDriver = async () => {
    setLoading(true);
    try {
      const response = await axios.post(`/shipments/${shipmentId}/smart-assign`, {
        useAutoAssignment: true,
        priority: selectedPriority
      });
      setAssignedDriver(response.data);
      alert(`Driver ${response.data.driver.fullName} assigned successfully!`);
    } catch (error) {
      console.error('Failed to assign driver:', error);
      alert('Failed to assign driver. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  // Manually assign selected driver
  const assignSpecificDriver = async (driverId) => {
    setLoading(true);
    try {
      const response = await axios.post(`/shipments/${shipmentId}/smart-assign`, {
        preferredDriverId: driverId,
        priority: selectedPriority
      });
      setAssignedDriver(response.data);
      alert(`Driver ${response.data.driver.fullName} assigned successfully!`);
    } catch (error) {
      console.error('Failed to assign driver:', error);
      alert('Failed to assign driver. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchRecommendations();
  }, [selectedPriority]);

  const getScoreColor = (score) => {
    if (score >= 0.8) return 'text-green-600';
    if (score >= 0.6) return 'text-blue-600';
    if (score >= 0.4) return 'text-yellow-600';
    return 'text-red-600';
  };

  const getScoreLabel = (score) => {
    if (score >= 0.8) return 'Excellent';
    if (score >= 0.6) return 'Good';
    if (score >= 0.4) return 'Fair';
    return 'Poor';
  };

  return (
    <div className="max-w-4xl mx-auto p-6">
      <h2 className="text-2xl font-bold mb-6">Smart Driver Assignment</h2>
      
      {/* Priority Selection */}
      <div className="mb-6">
        <label className="block text-sm font-medium mb-2">Assignment Priority:</label>
        <select
          value={selectedPriority}
          onChange={(e) => setSelectedPriority(e.target.value)}
          className="border rounded px-3 py-2"
        >
          <option value="Balanced">Balanced (Recommended)</option>
          <option value="Distance">Distance (Fastest)</option>
          <option value="Experience">Experience (Reliable)</option>
          <option value="Rating">Rating (Quality)</option>
          <option value="Availability">Availability (Load Balance)</option>
        </select>
      </div>

      {/* Auto Assignment Button */}
      <div className="mb-6">
        <button
          onClick={autoAssignDriver}
          disabled={loading || recommendations.length === 0}
          className="bg-blue-500 text-white px-6 py-2 rounded hover:bg-blue-600 disabled:opacity-50"
        >
          {loading ? 'Assigning...' : 'Auto-Assign Best Driver'}
        </button>
      </div>

      {/* Driver Recommendations */}
      <div className="space-y-4">
        <h3 className="text-lg font-semibold">Driver Recommendations</h3>
        
        {loading ? (
          <div>Loading recommendations...</div>
        ) : recommendations.length === 0 ? (
          <div className="text-red-600">No available drivers found for this shipment.</div>
        ) : (
          recommendations.map((rec, index) => (
            <div key={rec.driver.id} className="border rounded-lg p-4 bg-white shadow">
              <div className="flex justify-between items-start mb-2">
                <div>
                  <h4 className="font-semibold text-lg">
                    #{index + 1} {rec.driver.fullName}
                    {rec.driverDetails.isVerified && (
                      <span className="ml-2 text-xs bg-green-100 text-green-800 px-2 py-1 rounded">
                        Verified
                      </span>
                    )}
                  </h4>
                  <p className="text-gray-600">{rec.driver.email}</p>
                </div>
                <div className="text-right">
                  <div className={`text-2xl font-bold ${getScoreColor(rec.score)}`}>
                    {(rec.score * 100).toFixed(0)}%
                  </div>
                  <div className={`text-sm ${getScoreColor(rec.score)}`}>
                    {getScoreLabel(rec.score)}
                  </div>
                </div>
              </div>
              
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
                <div>
                  <span className="text-gray-500">Distance:</span>
                  <div className="font-medium">{rec.distance}km</div>
                </div>
                <div>
                  <span className="text-gray-500">Rating:</span>
                  <div className="font-medium">? {rec.rating}/5</div>
                </div>
                <div>
                  <span className="text-gray-500">Experience:</span>
                  <div className="font-medium">{rec.completedShipments} deliveries</div>
                </div>
                <div>
                  <span className="text-gray-500">Workload:</span>
                  <div className="font-medium">
                    {rec.activeShipments}/{rec.driverDetails.maxActiveShipments}
                  </div>
                </div>
              </div>

              <div className="mt-3 mb-3">
                <div className="text-sm text-gray-600">
                  <strong>Why recommended:</strong> {rec.recommendationReason}
                </div>
                {rec.recommendationFactors.length > 0 && (
                  <div className="mt-1 flex flex-wrap gap-1">
                    {rec.recommendationFactors.map((factor, i) => (
                      <span
                        key={i}
                        className="text-xs bg-blue-100 text-blue-800 px-2 py-1 rounded"
                      >
                        {factor}
                      </span>
                    ))}
                  </div>
                )}
              </div>

              <div className="flex justify-between items-center">
                <div className="text-sm text-gray-500">
                  Status: {rec.driverDetails.status} | 
                  Vehicle: {rec.driverDetails.vehicleType || 'Not specified'}
                </div>
                <button
                  onClick={() => assignSpecificDriver(rec.driver.id)}
                  disabled={loading}
                  className="bg-green-500 text-white px-4 py-2 rounded hover:bg-green-600 disabled:opacity-50"
                >
                  Assign This Driver
                </button>
              </div>
            </div>
          ))
        )}
      </div>

      {/* Assignment Result */}
      {assignedDriver && (
        <div className="mt-6 p-4 bg-green-100 border border-green-400 rounded">
          <h3 className="font-semibold text-green-800">Assignment Successful!</h3>
          <p>
            Driver <strong>{assignedDriver.driver.fullName}</strong> has been assigned to this shipment.
            Assignment Score: <strong>{(assignedDriver.score * 100).toFixed(0)}%</strong>
          </p>
        </div>
      )}
    </div>
  );
};

export default SmartDriverAssignment;
```

---

## Best Practices

### 1. Location Updates
- Drivers should update their current address when changing locations
- Use simple address strings instead of precise coordinates
- Implement location updates in driver mobile app with address input
- Address can be city, region, or more specific location descriptions

```javascript
// Driver location update
const updateDriverLocation = async (address) => {
  await axios.post('/api/drivers/location', {
    address: address
  });
};

// Example address updates
updateDriverLocation("Downtown Manhattan, NY");
updateDriverLocation("Los Angeles Distribution Center");
updateDriverLocation("Highway Rest Stop, Denver Area");
```

### 2. Status Management
- Automatically update driver status based on shipment assignments
- Allow drivers to manually set status (OnBreak, OffDuty)
- Reset to Available when completing final delivery

### 3. Working Hours
- Configure working hours to prevent assignments outside driver availability
- Support overnight shifts with proper time calculations
- Consider time zones for multi-region operations

### 4. Capacity Management
- Set realistic maximum active shipments per driver
- Adjust based on driver experience and vehicle capacity
- Monitor and adjust based on performance metrics

### 5. Assignment Strategies by Business Scenario

**Rush Deliveries:**
```json
{ "priority": "Distance", "useAutoAssignment": true }
```

**Premium Service:**
```json
{ "priority": "Rating", "useAutoAssignment": false }
```

**High Volume Days:**
```json
{ "priority": "Availability", "useAutoAssignment": true }
```

**Complex/Valuable Items:**
```json
{ "priority": "Experience", "useAutoAssignment": false }
```

### 6. Monitoring and Analytics
- Track assignment success rates by priority type
- Monitor average delivery times by driver scoring
- Analyze customer satisfaction correlation with driver ratings
- Identify optimal assignment strategies for different scenarios

---

## Troubleshooting

### Common Issues

**No Available Drivers Found:**
- Check driver status and working hours
- Verify location data is current
- Expand search radius if necessary
- Consider relaxing maximum active shipment limits temporarily

**Low Assignment Scores:**
- Review driver location accuracy
- Update driver ratings based on recent performance
- Verify driver availability status
- Check if drivers are within reasonable distance

**Assignment Failures:**
- Validate driver exists and has correct role
- Check driver capacity hasn't been exceeded
- Verify shipment isn't already assigned
- Ensure driver status is Available or Busy (not OffDuty/OnBreak)

### Performance Optimization

**Database Queries:**
- Index driver preferred regions and current addresses
- Cache driver availability status
- Use text-based region matching for assignment
- Implement connection pooling

**Real-time Updates:**
- Use WebSocket connections for live driver status
- Implement event-driven architecture for status changes
- Cache frequently accessed driver data
- Use background jobs for batch location updates

This smart driver assignment system provides a robust, scalable solution for optimizing driver-shipment matching while maintaining flexibility for different business requirements and scenarios.