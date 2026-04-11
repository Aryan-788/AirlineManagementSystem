# Airline Management System - Professional ER Diagram

This document provide a comprehensive overview of the data architecture for the Airline Management System. In this microservices ecosystem, data is partitioned across multiple service-specific databases. Relationships between services are maintained conceptually via **Soft References** (IDs) and synchronized through **Event-Driven Communication** (RabbitMQ).

## Entity Relationship Diagram

```mermaid
erDiagram
    %% --- IDENTITY SERVICE ---
    USER {
        int Id PK
        string Name
        string Email UK
        string PasswordHash
        string Role
        datetime CreatedAt
    }

    %% --- FLIGHT SERVICE ---
    FLIGHT {
        int Id PK
        string FlightNumber UK
        string Source
        string Destination
        datetime DepartureTime
        datetime ArrivalTime
        string Gate
        string Aircraft
        string Status
        int TotalSeats
        int EconomySeats
        int BusinessSeats
        int FirstSeats
    }

    FLIGHT_SCHEDULE {
        int Id PK
        int FlightId FK
        datetime DepartureTime
        datetime ArrivalTime
        string Gate
        string Status
        int TotalSeats
        int AvailableSeats
        int EconomySeats
        int BusinessSeats
        int FirstSeats
        decimal EconomyPrice
        decimal BusinessPrice
        decimal FirstClassPrice
    }

    %% --- BOOKING SERVICE ---
    BOOKING {
        int Id PK
        int UserId FK "Soft Ref to Identity"
        int FlightId FK "Soft Ref to Flight"
        int ScheduleId FK "Soft Ref to Schedule"
        string PNR UK
        decimal TotalAmount
        string Status
        string PaymentStatus
        datetime CreatedAt
        int TotalPassengers
    }

    PASSENGER {
        int Id PK
        int BookingId FK
        string Name
        int Age
        string Gender
        string PassportNumber
        string SeatNumber
        string SeatClass
        string Status
    }

    REFUND {
        int Id PK
        int BookingId FK
        int PassengerId FK
        int UserId FK
        decimal RefundAmount
        decimal RefundPercentage
        datetime CancellationTime
        datetime DepartureTime
        string RefundStatus
    }

    REFUND_POLICY {
        int Id PK
        int MinHoursBeforeDeparture
        int MaxHoursBeforeDeparture
        decimal RefundPercentage
    }

    %% --- PAYMENT SERVICE ---
    PAYMENT {
        int Id PK
        int BookingId FK "Soft Ref to Booking"
        decimal Amount
        string PaymentMethod
        string TransactionId UK
        string Status
        datetime CreatedAt
    }

    %% --- CHECK-IN SERVICE ---
    CHECKIN {
        int Id PK
        int BookingId FK "Soft Ref to Booking"
        int UserId FK
        int FlightId FK
        string SeatNumber
        string Gate
        string BoardingPass
        string QRCode
        datetime CheckInTime
        bool IsCheckedIn
    }

    %% --- BAGGAGE SERVICE ---
    BAGGAGE {
        int Id PK
        int BookingId FK "Soft Ref to Booking"
        decimal Weight
        string PassengerName
        string FlightNumber
        string Status
        bool IsDelivered
        string TrackingNumber UK
    }

    %% --- REWARD SERVICE ---
    REWARD {
        int Id PK
        int UserId FK "Soft Ref to Identity"
        int Points
        string TransactionType
        int BookingId FK
    }

    %% --- AGENT SERVICE ---
    DEALER {
        int Id PK
        string DealerName
        string DealerEmail UK
        int AllocatedSeats
        int UsedSeats
        decimal CommissionRate
        bool IsActive
    }

    DEALER_BOOKING {
        int Id PK
        int DealerId FK
        int BookingId FK
        int FlightId FK
        decimal Commission
    }

    %% --- NOTIFICATION SERVICE ---
    NOTIFICATION {
        int Id PK
        int UserId FK "Soft Ref to Identity"
        string Email
        string Subject
        string Message
        string NotificationType
        bool IsSent
        datetime CreatedAt
    }

    %% --- RELATIONSHIPS (Internal) ---
    FLIGHT ||--o{ FLIGHT_SCHEDULE : "has occurrences"
    BOOKING ||--|{ PASSENGER : "contains"
    BOOKING ||--o{ REFUND : "triggers"
    PASSENGER ||--o| REFUND : "referenced in"
    DEALER ||--o{ DEALER_BOOKING : "manages"

    %% --- RELATIONSHIPS (Cross-Service Conceptually) ---
    USER ||..o{ BOOKING : "places (Event-Sync)"
    FLIGHT_SCHEDULE ||..o{ BOOKING : "reserved for (Event-Sync)"
    BOOKING ||..|| PAYMENT : "paid by (Event-Sync)"
    BOOKING ||..|| CHECKIN : "registers (Event-Sync)"
    BOOKING ||..o{ BAGGAGE : "associates (Event-Sync)"
    USER ||..o{ REWARD : "earns (Event-Sync)"
    USER ||..o{ NOTIFICATION : "receives (Event-Sync)"
    DEALER_BOOKING ||..|| BOOKING : "records (Event-Sync)"
```

## Architectural Notes

### 🧩 Service Isolation
Each block in the diagram represents a distinct database schema managed by its respective microservice. Data integrity across these boundaries is **eventual**, maintained by the SAGA pattern and RabbitMQ integration.

### 🔄 Event-Driven State
- **Payment Success**: Updates `Booking.Status` and `Booking.PaymentStatus`.
- **Flight Delay**: Triggers `Notification` creation.
- **Cancellation**: Triggers `Refund` calculation and `FlightSchedule.AvailableSeats` update.

### 🔑 Key Legends
- **PK**: Primary Key
- **FK**: Foreign Key
- **UK**: Unique Key
- **Solid Line (-)**: Strong relationship within the same database.
- **Dotted Line (..)**: Conceptual relationship across different microservices.
