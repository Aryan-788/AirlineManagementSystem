# CQRS Architecture Diagram

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      API Clients                               │
└────────────────┬────────────────────────────────────────────────┘
                 │
┌────────────────▼────────────────────────────────────────────────┐
│                  BookingsController                             │
│  (HTTP Request Entry Point)                                     │
└────────────┬──────────────────────────────┬─────────────────────┘
             │                              │
    ┌────────▼─────────┐          ┌─────────▼────────┐
    │ Command Handlers │          │ Query Handlers   │
    └────────┬─────────┘          └─────────┬────────┘
             │                              │
    ┌────────▼─────────────────────────────┴────────┐
    │         Repository Layer                      │
    │  (IBookingRepository, IPassengerRepository)  │
    └────────┬─────────────────────────────────────┘
             │
    ┌────────▼─────────────────────────────────────┐
    │     BookingDbContext (EF Core)              │
    └────────┬─────────────────────────────────────┘
             │
    ┌────────▼─────────────────────────────────────┐
    │        SQL Server Database                   │
    └──────────────────────────────────────────────┘
```

## Command Flow (Write Operations)

```
HTTP POST Request
       │
       ▼
┌──────────────────────┐
│ BookingsController   │
└──────────┬───────────┘
           │
           ▼
┌──────────────────────────────────┐
│ Command Object Creation          │
│ (e.g., CreateBookingCommand)    │
└──────────┬───────────────────────┘
           │
           ▼
┌──────────────────────────────────┐
│ Command Handler                  │
│ (e.g., CreateBookingCommandH...)│
│                                  │
│ ✓ Validate input                │
│ ✓ Call repository methods       │
│ ✓ Perform business logic        │
│ ✓ Publish events                │
│ ✓ Return result                 │
└──────────┬───────────────────────┘
           │
           ▼
┌──────────────────────────────────┐
│ Repository                       │
│ (IBookingRepository)            │
└──────────┬───────────────────────┘
           │
           ▼
┌──────────────────────────────────┐
│ Database Update                  │
└──────────┬───────────────────────┘
           │
           ▼
┌──────────────────────────────────┐
│ Event Published (RabbitMQ)       │
│ (e.g., BookingCreatedEvent)     │
└──────────┬───────────────────────┘
           │
           ▼
┌──────────────────────────────────┐
│ HTTP Response (201 Created)      │
│ + Response DTO                   │
└──────────────────────────────────┘
```

## Query Flow (Read Operations)

```
HTTP GET Request
       │
       ▼
┌──────────────────────┐
│ BookingsController   │
└──────────┬───────────┘
           │
           ▼
┌──────────────────────────────────┐
│ Query Object Creation            │
│ (e.g., GetBookingByIdQuery)     │
└──────────┬───────────────────────┘
           │
           ▼
┌──────────────────────────────────┐
│ Query Handler                    │
│ (e.g., GetBookingByIdQueryH...)│
│                                  │
│ ✓ Retrieve data                 │
│ ✓ Map to DTO                    │
│ ✓ Return result                 │
└──────────┬───────────────────────┘
           │
           ▼
┌──────────────────────────────────┐
│ Repository                       │
│ (IBookingRepository)            │
└──────────┬───────────────────────┘
           │
           ▼
┌──────────────────────────────────┐
│ Database Query                   │
└──────────┬───────────────────────┘
           │
           ▼
┌──────────────────────────────────┐
│ HTTP Response (200 OK)           │
│ + Response DTO/Collection        │
└──────────────────────────────────┘
```

## Event-Driven Flow (RabbitMQ Integration)

```
┌─────────────────────────────────────────────────────────────────┐
│  Create Booking Command Handler                                │
│  ✓ Creates booking in database                                 │
│  ✓ Publishes BookingCreatedEvent                              │
└────────────┬────────────────────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────────────┐
│  RabbitMQ Message Bus                                          │
│  Topic: BookingService.Events                                  │
└────────────┬────────────────────────────────────────────────────┘
             │
    ┌────────┴─────────┬──────────────────┐
    │                  │                  │
    ▼                  ▼                  ▼
PaymentService   NotificationService  RewardService
subscribes       subscribes            subscribes
```

## Command Handlers Breakdown

```
┌─────────────────────────────────────────┐
│ Command Handlers (Write Operations)     │
├─────────────────────────────────────────┤
│                                         │
│ ✓ CreateBookingCommandHandler           │
│   └─ Validates flight/schedule          │
│   └─ Books seats                        │
│   └─ Publishes BookingCreatedEvent      │
│                                         │
│ ✓ CancelBookingCommandHandler           │
│   └─ Updates status to Cancelled        │
│   └─ Publishes BookingCancelledEvent    │
│                                         │
│ ✓ CreatePassengerCommandHandler         │
│   └─ Validates Aadhar uniqueness        │
│   └─ Updates booking counts             │
│                                         │
│ ✓ CancelPassengerCommandHandler         │
│   └─ Updates passenger status           │
│   └─ Updates booking counts             │
│                                         │
│ ✓ HandlePaymentSuccessCommandHandler    │
│   └─ Updates booking to Confirmed       │
│   └─ Publishes RewardEarnedEvent        │
│                                         │
│ ✓ HandlePaymentFailedCommandHandler     │
│   └─ Updates booking to Cancelled       │
│   └─ Publishes BookingCancelledEvent    │
│                                         │
└─────────────────────────────────────────┘
```

## Query Handlers Breakdown

```
┌─────────────────────────────────────────┐
│ Query Handlers (Read Operations)        │
├─────────────────────────────────────────┤
│                                         │
│ ✓ GetBookingByIdQueryHandler            │
│   └─ Returns single BookingDto          │
│                                         │
│ ✓ GetBookingHistoryQueryHandler         │
│   └─ Returns List<BookingHistoryDto>    │
│                                         │
│ ✓ GetBookingsByScheduleQueryHandler     │
│   └─ Returns List with passengers       │
│                                         │
│ ✓ GetOccupiedSeatsQueryHandler          │
│   └─ Returns List<string> (seats)       │
│                                         │
│ ✓ GetPassengersForBookingQueryHandler   │
│   └─ Returns List<PassengerResponseDto> │
│                                         │
└─────────────────────────────────────────┘
```

## Dependency Injection Container

```
┌──────────────────────────────────────────────┐
│      Service Provider (Program.cs)           │
├──────────────────────────────────────────────┤
│                                              │
│ Repositories (Scoped)                       │
│  - IBookingRepository                       │
│  - IPassengerRepository                     │
│                                              │
│ Services (Scoped) [Legacy Support]          │
│  - IBookingService                          │
│  - IPassengerService                        │
│                                              │
│ Command Handlers (Scoped)                   │
│  - CreateBookingCommandHandler              │
│  - CancelBookingCommandHandler              │
│  - CreatePassengerCommandHandler            │
│  - CancelPassengerCommandHandler            │
│  - HandlePaymentSuccessCommandHandler       │
│  - HandlePaymentFailedCommandHandler        │
│                                              │
│ Query Handlers (Scoped)                     │
│  - GetBookingByIdQueryHandler               │
│  - GetBookingHistoryQueryHandler            │
│  - GetBookingsByScheduleQueryHandler        │
│  - GetOccupiedSeatsQueryHandler             │
│  - GetPassengersForBookingQueryHandler      │
│                                              │
│ DbContext (Scoped)                          │
│  - BookingDbContext                         │
│                                              │
│ External Services (Singleton)               │
│  - IEventPublisher (RabbitMQ)               │
│  - IEventConsumer (RabbitMQ)                │
│  - HttpClient                               │
│  - IConnection (RabbitMQ)                   │
│                                              │
│ Authentication & Authorization              │
│  - JWT Bearer Token Validation              │
│  - Authorization Policies                   │
│                                              │
└──────────────────────────────────────────────┘
```

## Before vs After Comparison

### BEFORE (Service Layer Pattern)
```
Controller
    ↓
IBookingService / IPassengerService
    ├─ CreateBookingAsync()
    ├─ CancelBookingAsync()
    ├─ GetBookingAsync()
    ├─ CreatePassengerAsync()
    ├─ CancelPassengerAsync()
    ├─ GetPassengersForBookingAsync()
    ├─ GetBookingHistoryAsync()
    ├─ GetBookingsByScheduleAsync()
    ├─ GetOccupiedSeatsAsync()
    └─ Handle Payment Events
    ↓
Repository
    ↓
Database
```

### AFTER (CQRS Pattern)
```
Controller
    ├─ Command Path              ├─ Query Path
    │   ↓                        │   ↓
    │ CreateBookingCommand    GetBookingByIdQuery
    │   ↓                        │   ↓
    │ CreateBookingCommandH...  GetBookingByIdQueryH...
    │   ↓                        │   ↓
    │ Repository                 Repository
    │   ↓                        │   ↓
    │ Database                   Database
    │   ↓                        │   ↓
    │ Event Published            Result Returned
    └─────────┬──────────────────┘
            Response
```

## Data Flow Example: Create Booking

```
1. Client Request
   POST /api/bookings
   {
     "userId": 1,
     "flightId": 100,
     "scheduleId": 50,
     "seatClass": "Economy",
     "baggageWeight": 20,
     "passengerCount": 2,
     "totalAmount": 5000
   }

2. Controller Creates Command
   CreateBookingCommand cmd = new CreateBookingCommand(dto)

3. Handler Executes
   CreateBookingCommandHandler.HandleAsync(cmd)
   
   a. Validate with FlightService
   b. Create Booking entity
   c. Save to database via repository
   d. Publish BookingCreatedEvent to RabbitMQ
   e. Map Booking to BookingDto
   f. Return BookingDto

4. Response
   HTTP 201 Created
   {
     "id": 1001,
     "userId": 1,
     "flightId": 100,
     "scheduleId": 50,
     "seatClass": "Economy",
     "baggageWeight": 20,
     "pnr": "ABC123",
     "status": "Pending",
     "paymentStatus": "Pending",
     ...
   }

5. Event Propagation
   BookingCreatedEvent → RabbitMQ
     → PaymentService (if payment required)
     → NotificationService (booking confirmation email)
```
