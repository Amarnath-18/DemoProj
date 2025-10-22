# 🚛 Logistic Shipment Tracker - Project Flow Chart & Architecture

This document provides comprehensive visual flowcharts and architectural diagrams of the Logistic Shipment Tracker system, showing how all components interact and work together.

## Table of Contents
1. [System Overview](#system-overview)
2. [User Journey Flows](#user-journey-flows)
3. [Smart Driver Assignment Algorithm](#smart-driver-assignment-algorithm)
4. [Shipment Lifecycle Flow](#shipment-lifecycle-flow)
5. [Authentication & Authorization Flow](#authentication--authorization-flow)
6. [API Request Flow](#api-request-flow)
7. [Database Transaction Flow](#database-transaction-flow)
8. [Notification System Flow](#notification-system-flow)
9. [System Architecture](#system-architecture)

---

## System Overview

```mermaid
graph TB
    subgraph "Frontend Layer"
        A[React Frontend App]
        B[Mobile App - Future]
    end
    
    subgraph "API Layer"
        C[ASP.NET Core Web API]
        D[Controllers]
        E[Middleware]
    end
    
    subgraph "Business Logic Layer"
        F[Services]
        G[Driver Assignment Service]
        H[Notification Service]
        I[Report Service]
        J[Distance Service]
    end
    
    subgraph "Data Layer"
        K[(PostgreSQL Database)]
        L[Entity Framework Core]
        M[Repository Pattern]
    end
    
    subgraph "External Services"
        N[Nominatim API - Geocoding]
        O[SMS Service - Future]
        P[Email Service - Future]
    end
    
    A --> C
    B --> C
    C --> D
    D --> E
    D --> F
    F --> G
    F --> H
    F --> I
    F --> J
    G --> L
    H --> L
    I --> L
    L --> K
    J --> N
    H --> O
    H --> P
```

---

## User Journey Flows

### 1. Customer Journey - Creating a Shipment

```mermaid
sequenceDiagram
    participant C as Customer
    participant F as Frontend
    participant API as Backend API
    participant DB as Database
    participant DS as Driver Service
    participant NS as Notification Service
    
    C->>F: Login to system
    F->>API: POST /api/auth/login
    API->>DB: Validate credentials
    API->>F: Return user + auth cookie
    
    C->>F: Click "Create Shipment"
    F->>C: Show shipment form
    
    C->>F: Fill shipment details
    Note over C,F: Receiver info, pickup/delivery addresses
    
    F->>API: POST /api/shipments
    API->>DB: Create shipment record
    API->>DB: Generate tracking number
    
    API->>DS: Trigger smart assignment
    DS->>DB: Find available drivers
    DS->>DS: Calculate scores & rankings
    DS->>DB: Assign best driver
    
    API->>NS: Send notifications
    NS->>C: Email notification (tracking number)
    NS->>Driver: SMS notification (new assignment)
    
    API->>F: Return shipment details
    F->>C: Show success + tracking number
```

### 2. Driver Journey - Accepting and Completing Shipment

```mermaid
sequenceDiagram
    participant D as Driver
    participant F as Frontend/Mobile
    participant API as Backend API
    participant DB as Database
    participant NS as Notification Service
    
    D->>F: Login to driver app
    F->>API: POST /api/auth/login
    
    Note over D,F: Driver receives notification of new assignment
    
    D->>F: View assigned shipments
    F->>API: GET /api/shipments
    API->>DB: Get driver's shipments
    API->>F: Return shipment list
    
    D->>F: Update status to "PickedUp"
    F->>API: PUT /api/shipments/{id}/status
    API->>DB: Update shipment status
    API->>NS: Notify customer of pickup
    NS->>Customer: Send pickup notification
    
    D->>F: Update location during transit
    F->>API: POST /api/drivers/location
    API->>DB: Update driver location
    
    D->>F: Update status to "InTransit"
    F->>API: PUT /api/shipments/{id}/status
    API->>NS: Notify customer of transit
    
    D->>F: Update status to "Delivered"
    F->>API: PUT /api/shipments/{id}/status
    API->>DB: Complete shipment
    API->>NS: Notify customer of delivery
    NS->>Customer: Send delivery confirmation
```

### 3. Admin Journey - System Management

```mermaid
sequenceDiagram
    participant A as Admin
    participant F as Frontend
    participant API as Backend API
    participant DB as Database
    participant RS as Report Service
    
    A->>F: Login to admin panel
    F->>API: POST /api/auth/login
    
    A->>F: View dashboard
    F->>API: GET /api/reports/analytics
    API->>DB: Aggregate statistics
    API->>F: Return dashboard data
    
    A->>F: Generate monthly report
    F->>API: POST /api/reports/generate
    API->>RS: Generate PDF report
    RS->>DB: Query shipment data
    RS->>RS: Create PDF file
    RS->>DB: Save report metadata
    API->>F: Return report details
    
    A->>F: Manage driver verification
    F->>API: GET /api/drivers/availability
    API->>DB: Get all drivers status
    API->>F: Return driver list
    
    A->>F: Verify/unverify driver
    F->>API: PUT /api/drivers/{id}/verification
    API->>DB: Update driver verification
```

---

## Smart Driver Assignment Algorithm

```mermaid
flowchart TD
    A[New Shipment Created] --> B[Find Available Drivers]
    
    B --> C{Any Available Drivers?}
    C -->|No| D[Keep Shipment Unassigned<br/>Notify Admin]
    C -->|Yes| E[For Each Available Driver]
    
    E --> F[Calculate Distance Score<br/>40% weight]
    F --> G[Calculate Experience Score<br/>25% weight]
    G --> H[Calculate Rating Score<br/>20% weight]
    H --> I[Calculate Availability Score<br/>15% weight]
    
    I --> J[Combined Score = <br/>0.4×Distance + 0.25×Experience + <br/>0.2×Rating + 0.15×Availability]
    
    J --> K{More Drivers to Process?}
    K -->|Yes| E
    K -->|No| L[Sort Drivers by Score<br/>Highest First]
    
    L --> M[Select Best Driver<br/>Score > 0.5]
    
    M --> N{Best Driver Found?}
    N -->|No| O[Keep Unassigned<br/>Admin Review Required]
    N -->|Yes| P[Assign Driver to Shipment]
    
    P --> Q[Update Driver Status]
    Q --> R[Send Notifications]
    R --> S[Log Assignment Decision]
    
    style A fill:#e1f5fe
    style P fill:#c8e6c9
    style D fill:#ffcdd2
    style O fill:#ffcdd2
```

### Driver Scoring Algorithm Details

```mermaid
graph TD
    subgraph "Distance Score (40%)"
        A1[Calculate Distance<br/>Driver → Pickup Location]
        A2{Distance < 5km?}
        A3[Score = 1.0]
        A4{Distance < 15km?}
        A5[Score = 0.8]
        A6{Distance < 30km?}
        A7[Score = 0.5]
        A8[Score = 0.1]
        
        A1 --> A2
        A2 -->|Yes| A3
        A2 -->|No| A4
        A4 -->|Yes| A5
        A4 -->|No| A6
        A6 -->|Yes| A7
        A6 -->|No| A8
    end
    
    subgraph "Experience Score (25%)"
        B1[Get Completed Shipments]
        B2{Shipments > 100?}
        B3[Score = 1.0]
        B4{Shipments > 50?}
        B5[Score = 0.8]
        B6{Shipments > 20?}
        B7[Score = 0.6]
        B8{Shipments > 5?}
        B9[Score = 0.4]
        B10[Score = 0.2]
        
        B1 --> B2
        B2 -->|Yes| B3
        B2 -->|No| B4
        B4 -->|Yes| B5
        B4 -->|No| B6
        B6 -->|Yes| B7
        B6 -->|No| B8
        B8 -->|Yes| B9
        B8 -->|No| B10
    end
    
    subgraph "Rating Score (20%)"
        C1[Get Average Rating]
        C2{Rating ≥ 4.5?}
        C3[Score = 1.0]
        C4{Rating ≥ 4.0?}
        C5[Score = 0.8]
        C6{Rating ≥ 3.5?}
        C7[Score = 0.6]
        C8{Rating ≥ 3.0?}
        C9[Score = 0.4]
        C10[Score = 0.2]
        
        C1 --> C2
        C2 -->|Yes| C3
        C2 -->|No| C4
        C4 -->|Yes| C5
        C4 -->|No| C6
        C6 -->|Yes| C7
        C6 -->|No| C8
        C8 -->|Yes| C9
        C8 -->|No| C10
    end
    
    subgraph "Availability Score (15%)"
        D1[Check Current Load]
        D2{No Active Shipments?}
        D3[Score = 1.0]
        D4{Less than 50% capacity?}
        D5[Score = 0.8]
        D6{Less than 80% capacity?}
        D7[Score = 0.5]
        D8[Score = 0.2]
        
        D1 --> D2
        D2 -->|Yes| D3
        D2 -->|No| D4
        D4 -->|Yes| D5
        D4 -->|No| D6
        D6 -->|Yes| D7
        D6 -->|No| D8
    end
```

---

## Shipment Lifecycle Flow

```mermaid
stateDiagram-v2
    [*] --> Created: Customer creates shipment
    
    Created --> PickedUp: Driver picks up package
    Created --> Cancelled: Customer/Admin cancels
    
    PickedUp --> InTransit: Driver starts delivery
    PickedUp --> Cancelled: Issues during pickup
    
    InTransit --> Delivered: Successful delivery
    InTransit --> Cancelled: Delivery issues
    
    Delivered --> [*]: Shipment complete
    Cancelled --> [*]: Shipment terminated
    
    note right of Created
        - Auto-assign driver
        - Generate tracking number
        - Send notifications
    end note
    
    note right of PickedUp
        - Update customer
        - Track location
        - Start GPS tracking
    end note
    
    note right of InTransit
        - Real-time tracking
        - Location updates
        - ETA calculations
    end note
    
    note right of Delivered
        - Delivery confirmation
        - Enable rating
        - Generate receipt
    end note
```

---

## Authentication & Authorization Flow

```mermaid
sequenceDiagram
    participant U as User
    participant F as Frontend
    participant A as Auth Controller
    participant M as Auth Middleware
    participant T as Token Service
    participant DB as Database
    
    U->>F: Enter credentials
    F->>A: POST /api/auth/login
    A->>DB: Validate user credentials
    
    alt Valid Credentials
        DB->>A: User found & password valid
        A->>T: Generate JWT token
        T->>A: Return token
        A->>A: Set HTTP-only cookie
        A->>F: Return user data + cookie
        F->>U: Redirect to dashboard
    else Invalid Credentials
        DB->>A: Invalid credentials
        A->>F: Return error
        F->>U: Show error message
    end
    
    Note over F,A: All subsequent requests include cookie
    
    U->>F: Access protected resource
    F->>A: API request with cookie
    A->>M: Check authentication
    M->>T: Validate token from cookie
    
    alt Valid Token
        T->>M: Token valid + user claims
        M->>A: Allow request
        A->>DB: Process request
        DB->>A: Return data
        A->>F: Return response
    else Invalid/Expired Token
        T->>M: Token invalid
        M->>F: Return 401 Unauthorized
        F->>U: Redirect to login
    end
```

---

## API Request Flow

```mermaid
flowchart TD
    A[Client Request] --> B[ASP.NET Core Pipeline]
    B --> C[CORS Middleware]
    C --> D[Authentication Middleware]
    D --> E[Authorization Middleware]
    E --> F[Custom Cookie to Header Middleware]
    F --> G[Controller Action]
    
    G --> H{Validation}
    H -->|Invalid| I[Return 400 Bad Request]
    H -->|Valid| J[Service Layer]
    
    J --> K[Business Logic Processing]
    K --> L[Data Access Layer]
    L --> M[(Database)]
    
    M --> N[Entity Framework Core]
    N --> O[Return Data]
    O --> P[DTO Mapping]
    P --> Q[Response Formatting]
    Q --> R[Send Response]
    
    style A fill:#e3f2fd
    style G fill:#fff3e0
    style J fill:#f3e5f5
    style M fill:#e8f5e8
    style R fill:#e1f5fe
```

---

## Database Transaction Flow

```mermaid
sequenceDiagram
    participant C as Controller
    participant S as Service
    participant EF as Entity Framework
    participant DB as PostgreSQL
    
    C->>S: Business operation request
    S->>EF: Begin transaction
    EF->>DB: BEGIN TRANSACTION
    
    S->>EF: Create/Update entities
    EF->>EF: Track changes
    
    S->>EF: SaveChanges()
    EF->>DB: Execute SQL commands
    
    alt Success
        DB->>EF: Commands successful
        EF->>DB: COMMIT TRANSACTION
        EF->>S: Changes saved
        S->>C: Operation successful
    else Error
        DB->>EF: SQL error
        EF->>DB: ROLLBACK TRANSACTION
        EF->>S: Exception thrown
        S->>C: Handle error + return error response
    end
```

---

## Notification System Flow

```mermaid
flowchart TD
    A[Shipment Status Change] --> B[Notification Service]
    B --> C{Determine Recipients}
    
    C --> D[Customer Email]
    C --> E[Driver SMS - Future]
    C --> F[Admin Dashboard Alert]
    
    D --> G[Format Email Message]
    E --> H[Format SMS Message]
    F --> I[Create Dashboard Notification]
    
    G --> J[Send Email]
    H --> K[Send SMS - Future]
    I --> L[Store Notification]
    
    J --> M[Log Email Status]
    K --> N[Log SMS Status]
    L --> O[Update Dashboard]
    
    M --> P[(Notification Log)]
    N --> P
    O --> P
    
    style A fill:#e3f2fd
    style B fill:#fff3e0
    style P fill:#e8f5e8
```

---

## System Architecture

### High-Level Architecture

```mermaid
graph TB
    subgraph "Client Layer"
        A[React Web App]
        B[Mobile App - Future]
        C[Admin Dashboard]
    end
    
    subgraph "API Gateway - Future"
        D[Load Balancer]
        E[Rate Limiting]
        F[API Versioning]
    end
    
    subgraph "Application Layer"
        G[ASP.NET Core Web API]
        H[Controllers]
        I[Middleware Stack]
        J[Authentication/Authorization]
    end
    
    subgraph "Business Logic Layer"
        K[Services]
        L[Driver Assignment Engine]
        M[Notification Engine]
        N[Reporting Engine]
        O[Distance Calculation]
    end
    
    subgraph "Data Access Layer"
        P[Entity Framework Core]
        Q[Repository Pattern]
        R[Unit of Work]
    end
    
    subgraph "Database Layer"
        S[(PostgreSQL)]
        T[Database Migrations]
        U[Backup & Recovery]
    end
    
    subgraph "External Services"
        V[Nominatim Geocoding API]
        W[SMS Gateway - Future]
        X[Email Service - Future]
        Y[File Storage]
    end
    
    subgraph "Infrastructure"
        Z[Docker Containers - Future]
        AA[CI/CD Pipeline - Future]
        BB[Monitoring - Future]
    end
    
    A --> D
    B --> D
    C --> D
    D --> G
    G --> H
    H --> I
    I --> J
    J --> K
    K --> L
    K --> M
    K --> N
    K --> O
    L --> P
    M --> P
    N --> P
    O --> V
    P --> Q
    Q --> R
    R --> S
    S --> T
    S --> U
    M --> W
    M --> X
    N --> Y
```

### Database Architecture

```mermaid
erDiagram
    Users ||--o{ Shipments : "sends (SenderId)"
    Users ||--o{ Shipments : "delivers (AssignedDriverId)"
    Users ||--o{ Drivers : "extends (UserId)"
    Users ||--o{ TrackingUpdates : "creates"
    Users ||--o{ Reports : "generates"
    Users ||--o{ AuditLogs : "performs"
    Users ||--o{ DriverRatings : "rates as customer"
    Users ||--o{ DriverRatings : "rated as driver"
    
    Shipments ||--o{ TrackingUpdates : "has"
    Shipments ||--o{ Notifications : "triggers"
    Shipments ||--|| DriverRatings : "can be rated"
    
    Drivers ||--o{ DriverRatings : "receives"
    
    Users {
        uuid Id PK
        string FullName
        string Email UK
        string PasswordHash
        string Phone
        enum Role
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    Drivers {
        uuid UserId PK, FK
        string CurrentAddress
        enum Status
        datetime LastLocationUpdate
        int MaxActiveShipments
        decimal Rating
        int CompletedShipments
        int TotalRatings
        string VehicleType
        string LicenseNumber
        bool IsVerified
        datetime LastActiveTime
        time WorkStartTime
        time WorkEndTime
        string PreferredRegion
    }
    
    Shipments {
        uuid Id PK
        string TrackingNumber UK
        uuid SenderId FK
        string ReceiverName
        string ReceiverEmail
        string ReceiverPhone
        string OriginAddress
        string DestinationAddress
        string OriginCity
        string OriginRegion
        string DestinationCity
        string DestinationRegion
        enum Status
        uuid AssignedDriverId FK
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    TrackingUpdates {
        uuid Id PK
        uuid ShipmentId FK
        string Status
        string Location
        string Remarks
        datetime Timestamp
        uuid UpdatedBy FK
    }
    
    DriverRatings {
        uuid Id PK
        uuid DriverId FK
        uuid CustomerId FK
        uuid ShipmentId FK
        int Rating
        string Comment
        datetime CreatedAt
    }
    
    Notifications {
        uuid Id PK
        uuid ShipmentId FK
        enum Type
        string Recipient
        string Message
        enum Status
        datetime SentAt
    }
    
    Reports {
        uuid Id PK
        uuid GeneratedBy FK
        enum ReportType
        datetime StartDate
        datetime EndDate
        string FilePath
        datetime GeneratedAt
    }
    
    AuditLogs {
        uuid Id PK
        uuid UserId FK
        string Action
        string EntityType
        string EntityId
        string Changes
        datetime Timestamp
    }
```

### Technology Stack

```mermaid
graph TB
    subgraph "Frontend Technologies"
        A[React 18+]
        B[TypeScript]
        C[Material-UI / Tailwind CSS]
        D[Axios for HTTP]
        E[React Router]
        F[React Query - Future]
    end
    
    subgraph "Backend Technologies"
        G[ASP.NET Core 8]
        H[C# 12]
        I[Entity Framework Core 8]
        J[JWT Authentication]
        K[AutoMapper - Future]
        L[FluentValidation - Future]
    end
    
    subgraph "Database Technologies"
        M[PostgreSQL 15+]
        N[Entity Framework Migrations]
        O[LINQ Queries]
        P[Database Indexes]
    end
    
    subgraph "External APIs"
        Q[Nominatim Geocoding]
        R[SMS Gateway - Future]
        S[Email Service - Future]
    end
    
    subgraph "DevOps & Infrastructure"
        T[Git Version Control]
        U[Docker - Future]
        V[CI/CD - Future]
        W[Cloud Deployment - Future]
    end
    
    A --> G
    G --> M
    G --> Q
    M --> T
    G --> T
```

---

## Key System Flows Summary

1. **User Authentication**: Cookie-based authentication with JWT tokens
2. **Shipment Creation**: Customer creates → System auto-assigns driver → Notifications sent
3. **Smart Assignment**: Multi-factor algorithm considers distance, experience, rating, availability
4. **Status Tracking**: Real-time updates throughout shipment lifecycle
5. **Driver Management**: Profile management, location updates, status changes
6. **Analytics & Reporting**: Comprehensive dashboard with PDF report generation
7. **Data Persistence**: PostgreSQL with Entity Framework Core ORM
8. **External Integration**: Nominatim API for geocoding and distance calculations

This architecture ensures scalability, maintainability, and a smooth user experience across all system components.