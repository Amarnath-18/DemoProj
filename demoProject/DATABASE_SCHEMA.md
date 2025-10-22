# 🗄️ Database Schema Documentation - Logistic Shipment Tracker

This document provides comprehensive documentation of the database schema, including entity relationships, constraints, data types, and business rules.

## Table of Contents
1. [Database Overview](#database-overview)
2. [Entity Relationship Diagram](#entity-relationship-diagram)
3. [Table Definitions](#table-definitions)
4. [Relationships & Foreign Keys](#relationships--foreign-keys)
5. [Indexes & Constraints](#indexes--constraints)
6. [Enums & Data Types](#enums--data-types)
7. [Business Rules & Constraints](#business-rules--constraints)
8. [Migration History](#migration-history)

---

## Database Overview

**Database Type:** PostgreSQL 15+  
**ORM:** Entity Framework Core 8  
**Schema Name:** public  
**Total Tables:** 8  
**Character Set:** UTF-8  
**Time Zone:** All timestamps stored in UTC  

### Database Statistics
- **Entities:** 8 main entities
- **Relationships:** 12 foreign key relationships
- **Indexes:** 15+ indexes for performance
- **Constraints:** 25+ validation constraints
- **Triggers:** None (handled in application layer)

---

## Entity Relationship Diagram

```mermaid
erDiagram
    Users ||--o{ Shipments : "sends (SenderId)"
    Users ||--o{ Shipments : "delivers (AssignedDriverId)"
    Users ||--|| Drivers : "extends (UserId)"
    Users ||--o{ TrackingUpdates : "creates (UpdatedBy)"
    Users ||--o{ Reports : "generates (GeneratedBy)"
    Users ||--o{ AuditLogs : "performs (UserId)"
    Users ||--o{ DriverRatings : "rates_as_customer (CustomerId)"
    Users ||--o{ DriverRatings : "rated_as_driver (DriverId)"
    
    Shipments ||--o{ TrackingUpdates : "has (ShipmentId)"
    Shipments ||--o{ Notifications : "triggers (ShipmentId)"
    Shipments ||--o| DriverRatings : "can_be_rated (ShipmentId)"
    
    Users {
        uuid Id PK "Primary Key"
        string FullName "Required, Max 100"
        string Email UK "Unique, Required, Max 100"
        string PasswordHash "Required, Max 255"
        string Phone "Optional, Max 15"
        UserRole Role "Enum: Admin|Driver|Customer"
        timestamp CreatedAt "UTC, Auto-generated"
        timestamp UpdatedAt "UTC, Auto-updated"
    }
    
    Drivers {
        uuid UserId PK_FK "Primary Key & Foreign Key to Users"
        string CurrentAddress "Optional, Max 255"
        DriverStatus Status "Enum: Available|Busy|OffDuty|OnBreak"
        timestamp LastLocationUpdate "Optional, UTC"
        int MaxActiveShipments "Default: 5, Range: 1-10"
        decimal Rating "Range: 0-5, Default: 0"
        int CompletedShipments "Default: 0"
        int TotalRatings "Default: 0"
        string VehicleType "Optional, Max 50"
        string LicenseNumber "Optional, Max 20"
        bool IsVerified "Default: false"
        timestamp LastActiveTime "Optional, UTC"
        time WorkStartTime "Optional, Format: HH:mm:ss"
        time WorkEndTime "Optional, Format: HH:mm:ss"
        string PreferredRegion "Optional, Max 50"
    }
    
    Shipments {
        uuid Id PK "Primary Key"
        string TrackingNumber UK "Unique, Auto-generated, Format: LST######"
        uuid SenderId FK "Foreign Key to Users, Required"
        string ReceiverName "Required, Max 100"
        string ReceiverEmail "Required, Valid Email, Max 100"
        string ReceiverPhone "Optional, Max 15"
        text OriginAddress "Required"
        text DestinationAddress "Required"
        string OriginCity "Optional, Max 100"
        string OriginRegion "Optional, Max 100"
        string DestinationCity "Optional, Max 100"
        string DestinationRegion "Optional, Max 100"
        ShipmentStatus Status "Enum: Created|PickedUp|InTransit|Delivered|Cancelled"
        uuid AssignedDriverId FK "Optional Foreign Key to Users"
        timestamp CreatedAt "UTC, Auto-generated"
        timestamp UpdatedAt "UTC, Auto-updated"
    }
    
    TrackingUpdates {
        uuid Id PK "Primary Key"
        uuid ShipmentId FK "Foreign Key to Shipments, Required"
        string Status "Required, Max 50"
        string Location "Optional, Max 255"
        string Remarks "Optional, Max 255"
        timestamp Timestamp "UTC, Auto-generated"
        uuid UpdatedBy FK "Foreign Key to Users, Required"
    }
    
    DriverRatings {
        uuid Id PK "Primary Key"
        uuid DriverId FK "Foreign Key to Users (Driver), Required"
        uuid CustomerId FK "Foreign Key to Users (Customer), Required"
        uuid ShipmentId FK "Foreign Key to Shipments, Required"
        int Rating "Required, Range: 1-5"
        string Comment "Optional, Max 500"
        timestamp CreatedAt "UTC, Auto-generated"
    }
    
    Notifications {
        uuid Id PK "Primary Key"
        uuid ShipmentId FK "Foreign Key to Shipments, Required"
        NotificationType Type "Enum: Email|SMS"
        string Recipient "Required, Max 100"
        text Message "Required"
        NotificationStatus Status "Enum: Sent|Failed|Pending"
        timestamp SentAt "Optional, UTC"
    }
    
    Reports {
        uuid Id PK "Primary Key"
        uuid GeneratedBy FK "Foreign Key to Users, Required"
        ReportType ReportType "Enum: Daily|Weekly|Monthly|Custom"
        timestamp StartDate "Required, UTC"
        timestamp EndDate "Required, UTC"
        string FilePath "Optional, Max 255"
        timestamp GeneratedAt "UTC, Auto-generated"
    }
    
    AuditLogs {
        uuid Id PK "Primary Key"
        uuid UserId FK "Foreign Key to Users, Required"
        string Action "Required, Max 100"
        string EntityType "Required, Max 50"
        string EntityId "Required, Max 50"
        text Changes "Optional"
        timestamp Timestamp "UTC, Auto-generated"
    }
```

---

## Table Definitions

### 1. Users Table
**Description:** Core user entity storing authentication and profile information.

| Column | Data Type | Constraints | Description |
|--------|-----------|-------------|-------------|
| `Id` | `uuid` | PRIMARY KEY, DEFAULT uuid_generate_v4() | Unique identifier |
| `FullName` | `varchar(100)` | NOT NULL | User's full name |
| `Email` | `varchar(100)` | NOT NULL, UNIQUE | Email address for login |
| `PasswordHash` | `varchar(255)` | NOT NULL | BCrypt hashed password |
| `Phone` | `varchar(15)` | NULL | Optional phone number |
| `Role` | `text` | NOT NULL, CHECK (Role IN ('Admin', 'Driver', 'Customer')) | User role |
| `CreatedAt` | `timestamp with time zone` | NOT NULL, DEFAULT NOW() | Account creation time |
| `UpdatedAt` | `timestamp with time zone` | NOT NULL, DEFAULT NOW() | Last update time |

**Indexes:**
- `IX_Users_Email` (UNIQUE) - Fast email lookups for authentication
- `IX_Users_Role` - Filter users by role
- `IX_Users_CreatedAt` - Sort users by registration date

### 2. Drivers Table
**Description:** Extended profile information for users with Driver role.

| Column | Data Type | Constraints | Description |
|--------|-----------|-------------|-------------|
| `UserId` | `uuid` | PRIMARY KEY, FOREIGN KEY REFERENCES Users(Id) ON DELETE CASCADE | Links to Users table |
| `CurrentAddress` | `varchar(255)` | NULL | Current driver location |
| `Status` | `text` | NOT NULL, CHECK (Status IN ('Available', 'Busy', 'OffDuty', 'OnBreak')) | Driver availability |
| `LastLocationUpdate` | `timestamp with time zone` | NULL | Last location update time |
| `MaxActiveShipments` | `integer` | NOT NULL, DEFAULT 5, CHECK (MaxActiveShipments BETWEEN 1 AND 10) | Max concurrent shipments |
| `Rating` | `decimal(3,2)` | NOT NULL, DEFAULT 0, CHECK (Rating BETWEEN 0 AND 5) | Average rating |
| `CompletedShipments` | `integer` | NOT NULL, DEFAULT 0, CHECK (CompletedShipments >= 0) | Total completed shipments |
| `TotalRatings` | `integer` | NOT NULL, DEFAULT 0, CHECK (TotalRatings >= 0) | Total number of ratings |
| `VehicleType` | `varchar(50)` | NULL | Vehicle type description |
| `LicenseNumber` | `varchar(20)` | NULL | Driver's license number |
| `IsVerified` | `boolean` | NOT NULL, DEFAULT false | Admin verification status |
| `LastActiveTime` | `timestamp with time zone` | NULL | Last activity timestamp |
| `WorkStartTime` | `time` | NULL | Work shift start time |
| `WorkEndTime` | `time` | NULL | Work shift end time |
| `PreferredRegion` | `varchar(50)` | NULL | Preferred working region |

**Indexes:**
- `IX_Drivers_Status` - Filter drivers by availability status
- `IX_Drivers_IsVerified` - Filter verified drivers
- `IX_Drivers_Rating` - Sort drivers by rating
- `IX_Drivers_LastActiveTime` - Track driver activity

### 3. Shipments Table
**Description:** Core shipment entity containing delivery information.

| Column | Data Type | Constraints | Description |
|--------|-----------|-------------|-------------|
| `Id` | `uuid` | PRIMARY KEY, DEFAULT uuid_generate_v4() | Unique identifier |
| `TrackingNumber` | `varchar(50)` | NOT NULL, UNIQUE | Auto-generated tracking number |
| `SenderId` | `uuid` | NOT NULL, FOREIGN KEY REFERENCES Users(Id) ON DELETE RESTRICT | Customer who created shipment |
| `ReceiverName` | `varchar(100)` | NOT NULL | Recipient's name |
| `ReceiverEmail` | `varchar(100)` | NOT NULL | Recipient's email |
| `ReceiverPhone` | `varchar(15)` | NULL | Recipient's phone number |
| `OriginAddress` | `text` | NOT NULL | Pickup address |
| `DestinationAddress` | `text` | NOT NULL | Delivery address |
| `OriginCity` | `varchar(100)` | NULL | Origin city |
| `OriginRegion` | `varchar(100)` | NULL | Origin region/state |
| `DestinationCity` | `varchar(100)` | NULL | Destination city |
| `DestinationRegion` | `varchar(100)` | NULL | Destination region/state |
| `Status` | `text` | NOT NULL, CHECK (Status IN ('Created', 'PickedUp', 'InTransit', 'Delivered', 'Cancelled')) | Current status |
| `AssignedDriverId` | `uuid` | NULL, FOREIGN KEY REFERENCES Users(Id) ON DELETE RESTRICT | Assigned driver |
| `CreatedAt` | `timestamp with time zone` | NOT NULL, DEFAULT NOW() | Creation time |
| `UpdatedAt` | `timestamp with time zone` | NOT NULL, DEFAULT NOW() | Last update time |

**Indexes:**
- `IX_Shipments_TrackingNumber` (UNIQUE) - Fast tracking lookups
- `IX_Shipments_SenderId` - Get customer's shipments
- `IX_Shipments_AssignedDriverId` - Get driver's assignments
- `IX_Shipments_Status` - Filter by shipment status
- `IX_Shipments_CreatedAt` - Sort by creation date
- `IX_Shipments_OriginCity` - Filter by origin location
- `IX_Shipments_DestinationCity` - Filter by destination location

### 4. TrackingUpdates Table
**Description:** Status updates and location tracking for shipments.

| Column | Data Type | Constraints | Description |
|--------|-----------|-------------|-------------|
| `Id` | `uuid` | PRIMARY KEY, DEFAULT uuid_generate_v4() | Unique identifier |
| `ShipmentId` | `uuid` | NOT NULL, FOREIGN KEY REFERENCES Shipments(Id) ON DELETE CASCADE | Associated shipment |
| `Status` | `varchar(50)` | NOT NULL | Status description |
| `Location` | `varchar(255)` | NULL | Current location |
| `Remarks` | `varchar(255)` | NULL | Additional notes |
| `Timestamp` | `timestamp with time zone` | NOT NULL, DEFAULT NOW() | Update time |
| `UpdatedBy` | `uuid` | NOT NULL, FOREIGN KEY REFERENCES Users(Id) ON DELETE RESTRICT | User who made update |

**Indexes:**
- `IX_TrackingUpdates_ShipmentId` - Get updates for a shipment
- `IX_TrackingUpdates_UpdatedBy` - Track user activity
- `IX_TrackingUpdates_Timestamp` - Sort updates chronologically

### 5. DriverRatings Table
**Description:** Customer ratings and feedback for drivers.

| Column | Data Type | Constraints | Description |
|--------|-----------|-------------|-------------|
| `Id` | `uuid` | PRIMARY KEY, DEFAULT uuid_generate_v4() | Unique identifier |
| `DriverId` | `uuid` | NOT NULL, FOREIGN KEY REFERENCES Users(Id) ON DELETE CASCADE | Rated driver |
| `CustomerId` | `uuid` | NOT NULL, FOREIGN KEY REFERENCES Users(Id) ON DELETE CASCADE | Rating customer |
| `ShipmentId` | `uuid` | NOT NULL, FOREIGN KEY REFERENCES Shipments(Id) ON DELETE CASCADE | Associated shipment |
| `Rating` | `integer` | NOT NULL, CHECK (Rating BETWEEN 1 AND 5) | Numeric rating (1-5) |
| `Comment` | `varchar(500)` | NULL | Optional feedback text |
| `CreatedAt` | `timestamp with time zone` | NOT NULL, DEFAULT NOW() | Rating creation time |

**Indexes:**
- `IX_DriverRatings_DriverId` - Get ratings for a driver
- `IX_DriverRatings_ShipmentId` - Check if shipment is rated
- `IX_DriverRatings_CustomerId_ShipmentId` (UNIQUE) - Prevent duplicate ratings

### 6. Notifications Table
**Description:** Notification log for emails and SMS messages.

| Column | Data Type | Constraints | Description |
|--------|-----------|-------------|-------------|
| `Id` | `uuid` | PRIMARY KEY, DEFAULT uuid_generate_v4() | Unique identifier |
| `ShipmentId` | `uuid` | NOT NULL, FOREIGN KEY REFERENCES Shipments(Id) ON DELETE CASCADE | Related shipment |
| `Type` | `text` | NOT NULL, CHECK (Type IN ('Email', 'SMS')) | Notification method |
| `Recipient` | `varchar(100)` | NOT NULL | Recipient address/number |
| `Message` | `text` | NOT NULL | Notification content |
| `Status` | `text` | NOT NULL, CHECK (Status IN ('Sent', 'Failed', 'Pending')) | Delivery status |
| `SentAt` | `timestamp with time zone` | NULL | Actual send time |

**Indexes:**
- `IX_Notifications_ShipmentId` - Get notifications for shipment
- `IX_Notifications_Status` - Filter by delivery status
- `IX_Notifications_Type` - Filter by notification type

### 7. Reports Table
**Description:** Generated report metadata and file information.

| Column | Data Type | Constraints | Description |
|--------|-----------|-------------|-------------|
| `Id` | `uuid` | PRIMARY KEY, DEFAULT uuid_generate_v4() | Unique identifier |
| `GeneratedBy` | `uuid` | NOT NULL, FOREIGN KEY REFERENCES Users(Id) ON DELETE RESTRICT | Report creator |
| `ReportType` | `text` | NOT NULL, CHECK (ReportType IN ('Daily', 'Weekly', 'Monthly', 'Custom')) | Report type |
| `StartDate` | `timestamp with time zone` | NOT NULL | Report period start |
| `EndDate` | `timestamp with time zone` | NOT NULL | Report period end |
| `FilePath` | `varchar(255)` | NULL | Generated file location |
| `GeneratedAt` | `timestamp with time zone` | NOT NULL, DEFAULT NOW() | Generation time |

**Indexes:**
- `IX_Reports_GeneratedBy` - Get user's reports
- `IX_Reports_ReportType` - Filter by report type
- `IX_Reports_GeneratedAt` - Sort by generation date

### 8. AuditLogs Table
**Description:** Audit trail for system activities and changes.

| Column | Data Type | Constraints | Description |
|--------|-----------|-------------|-------------|
| `Id` | `uuid` | PRIMARY KEY, DEFAULT uuid_generate_v4() | Unique identifier |
| `UserId` | `uuid` | NOT NULL, FOREIGN KEY REFERENCES Users(Id) ON DELETE RESTRICT | User who performed action |
| `Action` | `varchar(100)` | NOT NULL | Action description |
| `EntityType` | `varchar(50)` | NOT NULL | Type of entity affected |
| `EntityId` | `varchar(50)` | NOT NULL | ID of affected entity |
| `Changes` | `text` | NULL | JSON of changes made |
| `Timestamp` | `timestamp with time zone` | NOT NULL, DEFAULT NOW() | Action timestamp |

**Indexes:**
- `IX_AuditLogs_UserId` - Get user's actions
- `IX_AuditLogs_EntityType` - Filter by entity type
- `IX_AuditLogs_Timestamp` - Sort chronologically

---

## Relationships & Foreign Keys

### One-to-One Relationships
1. **Users ↔ Drivers** (1:1)
   - `Drivers.UserId` → `Users.Id`
   - CASCADE DELETE: Driver profile deleted when user deleted

### One-to-Many Relationships

1. **Users → Shipments (as Sender)** (1:N)
   - `Shipments.SenderId` → `Users.Id`
   - RESTRICT DELETE: Cannot delete user with active shipments

2. **Users → Shipments (as Driver)** (1:N)
   - `Shipments.AssignedDriverId` → `Users.Id`
   - RESTRICT DELETE: Cannot delete driver with assigned shipments

3. **Users → TrackingUpdates** (1:N)
   - `TrackingUpdates.UpdatedBy` → `Users.Id`
   - RESTRICT DELETE: Maintain audit trail

4. **Users → Reports** (1:N)
   - `Reports.GeneratedBy` → `Users.Id`
   - RESTRICT DELETE: Maintain report history

5. **Users → AuditLogs** (1:N)
   - `AuditLogs.UserId` → `Users.Id`
   - RESTRICT DELETE: Maintain audit trail

6. **Users → DriverRatings (as Driver)** (1:N)
   - `DriverRatings.DriverId` → `Users.Id`
   - CASCADE DELETE: Remove ratings when driver deleted

7. **Users → DriverRatings (as Customer)** (1:N)
   - `DriverRatings.CustomerId` → `Users.Id`
   - CASCADE DELETE: Remove ratings when customer deleted

8. **Shipments → TrackingUpdates** (1:N)
   - `TrackingUpdates.ShipmentId` → `Shipments.Id`
   - CASCADE DELETE: Remove updates when shipment deleted

9. **Shipments → Notifications** (1:N)
   - `Notifications.ShipmentId` → `Shipments.Id`
   - CASCADE DELETE: Remove notifications when shipment deleted

10. **Shipments → DriverRatings** (1:N)
    - `DriverRatings.ShipmentId` → `Shipments.Id`
    - CASCADE DELETE: Remove ratings when shipment deleted

---

## Indexes & Constraints

### Primary Key Indexes
All tables use UUID primary keys with default B-Tree indexes.

### Unique Constraints
- `Users.Email` - Prevent duplicate email addresses
- `Shipments.TrackingNumber` - Ensure unique tracking numbers
- `DriverRatings.CustomerId + ShipmentId` - Prevent duplicate ratings per shipment

### Performance Indexes

#### Users Table
```sql
CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_Role ON Users(Role);
CREATE INDEX IX_Users_CreatedAt ON Users(CreatedAt);
```

#### Drivers Table
```sql
CREATE INDEX IX_Drivers_Status ON Drivers(Status);
CREATE INDEX IX_Drivers_IsVerified ON Drivers(IsVerified);
CREATE INDEX IX_Drivers_Rating ON Drivers(Rating DESC);
CREATE INDEX IX_Drivers_LastActiveTime ON Drivers(LastActiveTime);
```

#### Shipments Table
```sql
CREATE UNIQUE INDEX IX_Shipments_TrackingNumber ON Shipments(TrackingNumber);
CREATE INDEX IX_Shipments_SenderId ON Shipments(SenderId);
CREATE INDEX IX_Shipments_AssignedDriverId ON Shipments(AssignedDriverId);
CREATE INDEX IX_Shipments_Status ON Shipments(Status);
CREATE INDEX IX_Shipments_CreatedAt ON Shipments(CreatedAt DESC);
CREATE INDEX IX_Shipments_OriginCity ON Shipments(OriginCity);
CREATE INDEX IX_Shipments_DestinationCity ON Shipments(DestinationCity);
```

#### TrackingUpdates Table
```sql
CREATE INDEX IX_TrackingUpdates_ShipmentId ON TrackingUpdates(ShipmentId);
CREATE INDEX IX_TrackingUpdates_UpdatedBy ON TrackingUpdates(UpdatedBy);
CREATE INDEX IX_TrackingUpdates_Timestamp ON TrackingUpdates(Timestamp DESC);
```

#### DriverRatings Table
```sql
CREATE INDEX IX_DriverRatings_DriverId ON DriverRatings(DriverId);
CREATE INDEX IX_DriverRatings_ShipmentId ON DriverRatings(ShipmentId);
CREATE UNIQUE INDEX IX_DriverRatings_CustomerId_ShipmentId ON DriverRatings(CustomerId, ShipmentId);
```

### Check Constraints

#### Drivers Table
```sql
ALTER TABLE Drivers ADD CONSTRAINT CK_Drivers_MaxActiveShipments 
CHECK (MaxActiveShipments BETWEEN 1 AND 10);

ALTER TABLE Drivers ADD CONSTRAINT CK_Drivers_Rating 
CHECK (Rating BETWEEN 0 AND 5);

ALTER TABLE Drivers ADD CONSTRAINT CK_Drivers_CompletedShipments 
CHECK (CompletedShipments >= 0);

ALTER TABLE Drivers ADD CONSTRAINT CK_Drivers_TotalRatings 
CHECK (TotalRatings >= 0);
```

#### DriverRatings Table
```sql
ALTER TABLE DriverRatings ADD CONSTRAINT CK_DriverRatings_Rating 
CHECK (Rating BETWEEN 1 AND 5);
```

---

## Enums & Data Types

### UserRole Enum
```csharp
public enum UserRole
{
    Admin,      // System administrator
    Driver,     // Delivery driver
    Customer    // Customer/shipper
}
```

### DriverStatus Enum
```csharp
public enum DriverStatus
{
    Available,  // Ready for new assignments
    Busy,       // Currently on deliveries
    OffDuty,    // Not working
    OnBreak     // Temporarily unavailable
}
```

### ShipmentStatus Enum
```csharp
public enum ShipmentStatus
{
    Created,    // Shipment created, awaiting pickup
    PickedUp,   // Package collected from sender
    InTransit,  // On the way to destination
    Delivered,  // Successfully delivered
    Cancelled   // Shipment cancelled
}
```

### NotificationType Enum
```csharp
public enum NotificationType
{
    Email,      // Email notification
    SMS         // SMS notification (future feature)
}
```

### NotificationStatus Enum
```csharp
public enum NotificationStatus
{
    Pending,    // Queued for sending
    Sent,       // Successfully sent
    Failed      // Failed to send
}
```

### ReportType Enum
```csharp
public enum ReportType
{
    Daily,      // Daily report
    Weekly,     // Weekly report
    Monthly,    // Monthly report
    Custom      // Custom date range
}
```

---

## Business Rules & Constraints

### User Management Rules
1. **Email Uniqueness**: Each email can only be used once across the system
2. **Role Restrictions**: Users cannot change their own role (Admin only)
3. **Deletion Rules**: Users with active shipments cannot be deleted
4. **Password Policy**: Passwords must be BCrypt hashed (handled in application)

### Driver Management Rules
1. **Driver Profile**: Only users with Driver role can have driver profiles
2. **Verification**: Only admins can verify/unverify drivers
3. **Rating Updates**: Driver ratings are automatically calculated from individual ratings
4. **Work Schedule**: Work times are optional and used for availability calculations
5. **Capacity Limits**: Drivers cannot exceed their maximum active shipments

### Shipment Management Rules
1. **Tracking Numbers**: Auto-generated in format "LST" + 6 random digits
2. **Status Progression**: Shipments follow a specific status flow
3. **Assignment Rules**: Only verified drivers can be assigned to shipments
4. **Delivery Confirmation**: Only assigned drivers can mark shipments as delivered
5. **Cancellation Policy**: Shipments can be cancelled before delivery

### Rating System Rules
1. **Single Rating**: Each customer can rate a driver only once per shipment
2. **Completed Shipments Only**: Ratings can only be given after delivery
3. **Rating Range**: Ratings must be between 1 and 5 stars
4. **Rating Updates**: Driver's average rating is recalculated after each new rating

### Notification Rules
1. **Automatic Triggers**: Notifications are sent on status changes
2. **Retry Logic**: Failed notifications are marked for retry (future feature)
3. **Recipient Validation**: Email addresses and phone numbers are validated

### Audit Trail Rules
1. **Complete Logging**: All significant actions are logged
2. **Immutable Records**: Audit logs cannot be modified or deleted
3. **User Attribution**: All actions are linked to the performing user
4. **Change Tracking**: Before/after values are stored for updates

---

## Migration History

### Migration: `20251019043543_initial`
**Date:** October 19, 2024  
**Description:** Initial database structure creation
- Created all core tables
- Established primary keys and foreign key relationships
- Added basic indexes and constraints

### Migration: `20251019074401_added on delete cascade`
**Date:** October 19, 2024  
**Description:** Updated foreign key relationships
- Changed delete behavior for better data integrity
- Added CASCADE deletes where appropriate
- Ensured audit trail preservation

### Migration: `20251019142956_driver seperated from user`
**Date:** October 19, 2024  
**Description:** Separated driver data from user table
- Created dedicated Drivers table
- Moved driver-specific fields from Users to Drivers
- Established 1:1 relationship between Users and Drivers

### Migration: `20251019155553_RemoveCoordinatesSimplifyAddresses`
**Date:** October 19, 2024  
**Description:** Simplified location handling
- Removed coordinate columns from various tables
- Simplified address storage to text fields
- Updated to use external geocoding service

### Migration: `20251019161635_RemoveCoordinateColumns`
**Date:** October 19, 2024  
**Description:** Final coordinate cleanup
- Removed remaining coordinate columns
- Cleaned up unused location-related fields

### Migration: `20251020155450_AddDriverRatings`
**Date:** October 20, 2024  
**Description:** Added driver rating system
- Created DriverRatings table
- Added rating fields to Drivers table
- Established relationships for rating system

---

## Database Performance Considerations

### Query Optimization
1. **Indexes**: Strategic indexes on frequently queried columns
2. **Composite Indexes**: Multi-column indexes for complex queries
3. **Partial Indexes**: Consider partial indexes for status-based queries
4. **Query Plans**: Regular analysis of query execution plans

### Data Growth Planning
1. **Archival Strategy**: Plan for archiving old shipments and audit logs
2. **Partitioning**: Consider table partitioning for large tables (future)
3. **Cleanup Jobs**: Regular cleanup of old notifications and reports
4. **Backup Strategy**: Regular backups with point-in-time recovery

### Monitoring & Maintenance
1. **Index Usage**: Monitor index effectiveness and unused indexes
2. **Table Statistics**: Keep table statistics updated for query optimization
3. **Connection Pooling**: Proper connection pool configuration
4. **Deadlock Monitoring**: Monitor for deadlocks and optimize queries

---

## Security Considerations

### Data Protection
1. **Password Hashing**: BCrypt hashing for all passwords
2. **Sensitive Data**: Minimal storage of sensitive information
3. **PII Handling**: Proper handling of personally identifiable information
4. **Data Encryption**: Consider encryption for sensitive fields (future)

### Access Control
1. **Row-Level Security**: Application-level access control
2. **Database Users**: Separate database users for different access levels
3. **Connection Security**: Encrypted connections to database
4. **Audit Compliance**: Complete audit trail for compliance requirements

This comprehensive database schema supports the full functionality of the Logistic Shipment Tracker system while ensuring data integrity, performance, and scalability.