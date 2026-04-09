# Airline Management System - System Design Documentation

## 1. High Level Design (HLD)

The Airline Management System is built on a modern microservices architecture using .NET 10. It leverages Ocelot as the API Gateway for request routing and load balancing, RabbitMQ for asynchronous event-driven communication (Saga pattern), and separate SQL Server databases per service to ensure loose coupling and scalability. Angular serves as the frontend client.

### Authentication & Role-Based Access
- **Authentication Flow**: The user authenticates against the Identity Service. A JWT (JSON Web Token) is generated and returned to the client. The client passes this token in the `Authorization: Bearer <token>` header for all subsequent API Gateway requests.
- **Role-Based Access**: The system supports 4 primary roles: `Passenger`, `Admin`, `Dealer` (Agent), and `GroundStaff`.

### HLD Architecture Diagram
```mermaid
graph TD
    Client[Angular Frontend] -->|HTTP/JWT| AG[Ocelot API Gateway]
    
    subgraph Microservices
        AG --> IS[Identity Service]
        AG --> FS[Flight Service]
        AG --> BS[Booking Service]
        AG --> PS[Payment Service]
        AG --> RS[Reward Service]
        AG --> AS[Agent Service]
        AG --> CS[Check-In Service]
        AG --> BaS[Baggage Service]
        AG --> NS[Notification Service]
        AG --> AdS[Admin Service]
    end

    subgraph Databases
        IS --> DB_IS[(Identity DB)]
        FS --> DB_FS[(Flight DB)]
        BS --> DB_BS[(Booking DB)]
        PS --> DB_PS[(Payment DB)]
        RS --> DB_RS[(Reward DB)]
        AS --> DB_AS[(Agent DB)]
        CS --> DB_CS[(Check-In DB)]
        BaS --> DB_BaS[(Baggage DB)]
        NS --> DB_NS[(Notification DB)]
    end

    subgraph Event Bus
        RabbitMQ((RabbitMQ Event Bus))
    end

    BS <--> RabbitMQ
    PS <--> RabbitMQ
    RS <--> RabbitMQ
    NS <--> RabbitMQ
    FS <--> RabbitMQ
    CS <--> RabbitMQ
    BaS <--> RabbitMQ
```

## 2. Low Level Design (LLD)

### Identity Service
- **Controllers**: AuthController (Login, Register, GetProfile)
- **Models**: User (Id, Name, Email, PasswordHash, Role)
- **Flow**: Validates credentials, issues JWT containing Role and UserId claims.

### Flight Service
- **Models**: Flight (FlightNumber, Source, Destination, DepartureTime, ArrivalTime, Seats, Pricing)
- **Logic**: Manages flight schedules, searches flights based on source/dest/date, manages available seats across Economy/Business/First classes.

### Booking Service
- **Models**: Booking (UserId, FlightId, PNR, Status, SeatClass, Passengers)
- **Logic**: Generates unique PNR, calculates total passengers, reserves seats via API call to Flight Service, manages booking status.

### Payment Service
- **Models**: Payment (BookingId, Amount, Status, TransactionId)
- **Logic**: Integrates with Razorpay. Creates order, verifies HMAC signature, publishes `PaymentSuccessEvent` or `PaymentFailedEvent`.

### Reward Service
- **Models**: Reward (UserId, Points, TransactionType)
- **Logic**: Listens to `RewardEarnedEvent` from RabbitMQ. Calculates points earned, manages points redemption during booking.

### Check-In Service
- **Models**: CheckIn (BookingId, SeatNumber, BoardingPass, CheckInTime)
- **Logic**: Allows seat selection 24 hours before departure, generates Boarding Pass and QR Code, publishes `CheckInCompletedEvent`.

### Baggage Service
- **Models**: Baggage (BookingId, Weight, TrackingNumber, Status)
- **Logic**: Tracks luggage transition from Checked -> Loaded -> InTransit -> Delivered.

### Agent Service
- **Models**: Dealer (DealerName, CommissionRate), DealerBooking
- **Logic**: Allocates flight bulk seats to dealers, tracks dealer bookings, calculates and issues commission points.

### Notification Service
- **Models**: Notification (UserId, Email, Subject, Message)
- **Logic**: Pure consumer service. Listens to RabbitMQ events (BookingCreated, PaymentSuccess, FlightDelayed) and dispatches emails/alerts.

### Admin Service
- **Models**: AdminDashboard, RevenueReport
- **Logic**: Aggregates data from other services to generate reports on revenue, active flights, and system traffic.


## 3. Activity Diagram: Passenger Flight Booking Flow

```mermaid
stateDiagram-v2
    [*] --> SearchFlight : Enter Source, Dest, Date
    SearchFlight --> SelectFlight : View Results
    SelectFlight --> CreateBooking : Enter Passenger Details
    
    CreateBooking --> PendingPayment : Reserve Seats
    
    PendingPayment --> MakePayment : Redirect to Razorpay
    
    state MakePayment {
        [*] --> EnterCardDetails
        EnterCardDetails --> VerifySignature
        VerifySignature --> PaymentSuccess : Valid
        VerifySignature --> PaymentFailed : Invalid/Cancel
    }
    
    MakePayment --> ConfirmBooking : PaymentSuccess
    ConfirmBooking --> GeneratePNR
    GeneratePNR --> SendNotification
    GeneratePNR --> EarnRewardPoints
    EarnRewardPoints --> [*]
    
    MakePayment --> CancelBooking : PaymentFailed
    CancelBooking --> NotifyFailure
    NotifyFailure --> [*]
```

## 4. Sequence Diagram: Flight Booking with Payment

```mermaid
sequenceDiagram
    actor Passenger
    participant AG as API Gateway
    participant BS as Booking Service
    participant FS as Flight Service
    participant PS as Payment Service
    participant RMQ as RabbitMQ
    participant RS as Reward Service
    participant NS as Notification Service

    Passenger->>AG: Search Flights
    AG->>FS: GET /api/flights/search
    FS-->>AG: Flight List
    AG-->>Passenger: Display Flights
    
    Passenger->>AG: Create Booking
    AG->>BS: POST /api/bookings
    BS->>FS: Verify Seats & Reserve (HTTP)
    FS-->>BS: Seats Reserved
    BS-->>AG: Booking Created (Status = Pending)
    AG-->>Passenger: Booking summary
    
    Passenger->>AG: Request Payment Order
    AG->>PS: POST /api/payments/create-order
    PS-->>AG: orderId, amount, key
    AG-->>Passenger: Render Razorpay Checkout
    
    Passenger->>Passenger: Completes Razorpay Payment
    
    Passenger->>AG: Verify Payment Signature
    AG->>PS: POST /api/payments/verify
    PS->>PS: Verify HMAC Signature
    
    PS->>RMQ: Publish PaymentSuccessEvent
    PS-->>AG: Payment Verified 200 OK
    AG-->>Passenger: Payment Confirmation Screen
    
    RMQ-->>BS: Consume PaymentSuccessEvent
    BS->>BS: Update Booking Status = Confirmed
    
    RMQ-->>RS: Consume RewardEarnedEvent
    RS->>RS: Add Reward Points
    
    RMQ-->>NS: Consume PaymentSuccessEvent / BookingConfirmed
    NS->>NS: Send Email Notification
```

## 5. State Diagram: Booking Lifecycle

```mermaid
stateDiagram-v2
    [*] --> Created
    
    Created --> PendingPayment : Seats Reserved
    
    PendingPayment --> Confirmed : Payment Success
    PendingPayment --> Cancelled : Payment Failed
    
    Confirmed --> CheckedIn : Passenger Checks-In (Generated Boarding Pass)
    Confirmed --> Cancelled : Passenger Cancels Booking
    
    Cancelled --> Refunded : Admin/Agent initiates refund
    
    CheckedIn --> Completed : Flight Landed safely
    
    Completed --> [*]
    Refunded --> [*]
    Cancelled --> [*]
```

## 6. Microservices Architecture Diagram

```mermaid
graph TD
    classDef frontend fill:#e3f2fd,stroke:#1565c0,stroke-width:2px;
    classDef gateway fill:#fff3e0,stroke:#e65100,stroke-width:2px;
    classDef service fill:#e8f5e9,stroke:#2e7d32,stroke-width:2px;
    classDef msgbroker fill:#f3e5f5,stroke:#6a1b9a,stroke-width:2px;

    UI[Angular Frontend]:::frontend
    Ocelot(Ocelot API Gateway):::gateway
    
    UI -->|HTTPS / JWT| Ocelot
    
    subgraph Microservices Cluster
        Ocelot --> Identity[Identity Service]:::service
        Ocelot --> Flight[Flight Service]:::service
        Ocelot --> Booking[Booking Service]:::service
        Ocelot --> Payment[Payment Service]:::service
        Ocelot --> Reward[Reward Service]:::service
        Ocelot --> Agent[Agent Service]:::service
        Ocelot --> CheckIn[Check-In Service]:::service
        Ocelot --> Baggage[Baggage Service]:::service
        Ocelot --> Admin[Admin Service]:::service
    end
    
    RabbitMQ{{RabbitMQ Event Bus}}:::msgbroker
    Notification[Notification Service]:::service
    
    Booking -.->|Publishes| RabbitMQ
    Payment -.->|Publishes| RabbitMQ
    Flight -.->|Publishes| RabbitMQ
    CheckIn -.->|Publishes| RabbitMQ
    Baggage -.->|Publishes| RabbitMQ
    
    RabbitMQ -.->|Consumes| Reward
    RabbitMQ -.->|Consumes| Booking
    RabbitMQ -.->|Consumes| Notification
    RabbitMQ -.->|Consumes| Admin
```

## 7. Database Design

| Microservice | Table Name | Key Columns |
|--------------|------------|-------------|
| **Identity Service** | `Users` | Id (PK), Name, Email, PasswordHash, Role, CreatedAt |
| **Flight Service** | `Flights` | Id (PK), FlightNumber, Source, Destination, DepartureTime, EconomySeats, Prices |
| **Booking Service** | `Bookings` | Id (PK), UserId, FlightId, PNR, Status, PaymentStatus, SeatClass |
| | `Passengers` | Id (PK), BookingId (FK), Name, Age, Gender, AadharNumber, Status |
| **Payment Service** | `Payments` | Id (PK), BookingId, Amount, Status, PaymentMethod, TransactionId |
| **Reward Service** | `Rewards` | Id (PK), UserId, Points, TransactionType, BookingId |
| **Agent Service** | `Dealers` | Id (PK), DealerName, AllocatedSeats, CommissionRate |
| | `DealerBookings` | Id (PK), DealerId (FK), BookingId |
| **Check-In Service** | `CheckIns` | Id (PK), BookingId, SeatNumber, Gate, BoardingPass |
| **Baggage Service**| `Baggages` | Id (PK), BookingId, Weight, Status, TrackingNumber |

## 8. Event Driven Flow (RabbitMQ)

The system relies on a Saga pattern via RabbitMQ for eventual consistency. The main events are:

1. `BookingCreatedEvent` (Published by Booking) -> Consumed by Notification (Sends email).
2. `PaymentSuccessEvent` (Published by Payment) -> Consumed by Booking (Marks booking Confirmed), Notification (Sends receipt).
3. `PaymentFailedEvent` (Published by Payment) -> Consumed by Booking (Marks booking Cancelled), Notification.
4. `RewardEarnedEvent` (Published by Booking during payment success logic) -> Consumed by Reward (Credits points).
5. `BookingCancelledEvent` (Published by Booking) -> Consumed by Flight (Frees up seats).
6. `FlightDelayedEvent` (Published by Flight) -> Consumed by Notification (Notifies passengers).
7. `CheckInCompletedEvent` (Published by CheckIn) -> Consumed by Notification (Sends Boarding Pass).

## 9. Role Based Flow

- **Passenger Flow**:
  - Registers/Logs in securely.
  - Searches for a flight and inputs passenger detail constraints.
  - Generates a Pending booking, redirected to the secure Payment Checkout interface.
  - Earns rewards upon transaction completion and check-in success. Needs luggage tracking details dynamically pulled from baggage DB.
  
- **Admin Flow**:
  - Secure login with Global privileges.
  - Interacts with Admin/Dashboard Service to view aggregated metrics (Revenue, Load factors, Total users).
  - Can cancel flights or delay them from the Dashboard (Triggering RabbitMQ wide alerts).
  - Handles payment refund management.

- **Dealer (Agent) Flow**:
  - Authorized access to bulk seat allocation logic.
  - Receives automatic commission distributions upon confirmed proxy-bookings.
  - Dashboards reflect commissions earned, seats remaining in their agent account.
  
- **Ground Staff Flow**:
  - Utilizes Check-In & Baggage Services.
  - Confirms physical reporting, assigns baggage specific tracking numbers, updates baggage status to `Loaded`.
