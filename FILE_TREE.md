# Complete Airline Management System - File Tree

```
AirlineManagementSystem/
│
├── .gitignore
├── docker-compose.yml
├── README.md
├── QUICKSTART.md
├── API_DOCUMENTATION.md
├── BUILD_INSTRUCTIONS.md

├── Shared/
│   ├── Shared.csproj
│   ├── Models/
│   │   └── Enums.cs
│   ├── Events/
│   │   └── IntegrationEvents.cs
│   ├── Configuration/
│   │   └── Settings.cs
│   ├── Security/
│   │   └── JwtTokenService.cs
│   └── RabbitMQ/
│       └── EventPublisher.cs

├── Services/
│
│   ├── IdentityService/
│   │   ├── IdentityService.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── Dockerfile
│   │   ├── Models/
│   │   │   └── User.cs
│   │   ├── DTOs/
│   │   │   └── AuthDtos.cs
│   │   ├── Data/
│   │   │   └── IdentityDbContext.cs
│   │   ├── Repositories/
│   │   │   └── UserRepository.cs
│   │   ├── Services/
│   │   │   └── AuthService.cs
│   │   └── Controllers/
│   │       └── AuthController.cs
│
│   ├── FlightService/
│   │   ├── FlightService.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── Dockerfile
│   │   ├── Models/
│   │   │   └── Flight.cs
│   │   ├── DTOs/
│   │   │   └── FlightDtos.cs
│   │   ├── Data/
│   │   │   └── FlightDbContext.cs
│   │   ├── Repositories/
│   │   │   └── FlightRepository.cs
│   │   ├── Services/
│   │   │   └── FlightService.cs
│   │   └── Controllers/
│   │       └── FlightsController.cs
│
│   ├── BookingService/
│   │   ├── BookingService.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── Dockerfile
│   │   ├── CQRS/
│   │   │   ├── Commands/
│   │   │   ├── Handlers/
│   │   │   └── Queries/
│   │   ├── Models/
│   │   │   ├── Booking.cs
│   │   │   ├── Passenger.cs
│   │   │   ├── Refund.cs
│   │   │   └── RefundPolicy.cs
│   │   ├── DTOs/
│   │   │   ├── BookingDtos.cs
│   │   │   └── RefundDtos.cs
│   │   ├── Data/
│   │   │   └── BookingDbContext.cs
│   │   ├── Repositories/
│   │   │   └── BookingRepository.cs
│   │   ├── Services/
│   │   │   ├── BookingService.cs
│   │   │   ├── PassengerService.cs
│   │   │   └── RefundService.cs
│   │   └── Controllers/
│   │       └── BookingsController.cs
│
│   ├── PaymentService/
│   │   ├── PaymentService.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── Dockerfile
│   │   ├── Models/
│   │   │   └── Payment.cs
│   │   ├── DTOs/
│   │   │   └── PaymentDtos.cs
│   │   ├── Data/
│   │   │   └── PaymentDbContext.cs
│   │   ├── Repositories/
│   │   │   └── PaymentRepository.cs
│   │   ├── Services/
│   │   │   └── PaymentService.cs
│   │   └── Controllers/
│   │       └── PaymentsController.cs
│
│   ├── CheckInService/
│   │   ├── CheckInService.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── Dockerfile
│   │   ├── Models/
│   │   │   └── CheckIn.cs
│   │   ├── DTOs/
│   │   │   └── CheckInDtos.cs
│   │   ├── Data/
│   │   │   └── CheckInDbContext.cs
│   │   ├── Repositories/
│   │   │   └── CheckInRepository.cs
│   │   ├── Services/
│   │   │   └── CheckInService.cs
│   │   └── Controllers/
│   │       └── CheckInsController.cs
│
│   ├── BaggageService/
│   │   ├── BaggageService.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── Dockerfile
│   │   ├── Models/
│   │   │   └── Baggage.cs
│   │   ├── DTOs/
│   │   │   └── BaggageDtos.cs
│   │   ├── Data/
│   │   │   └── BaggageDbContext.cs
│   │   ├── Repositories/
│   │   │   └── BaggageRepository.cs
│   │   ├── Services/
│   │   │   └── BaggageService.cs
│   │   └── Controllers/
│   │       └── BaggagesController.cs
│
│   ├── RewardService/
│   │   ├── RewardService.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── Dockerfile
│   │   ├── Models/
│   │   │   └── Reward.cs
│   │   ├── DTOs/
│   │   │   └── RewardDtos.cs
│   │   ├── Data/
│   │   │   └── RewardDbContext.cs
│   │   ├── Repositories/
│   │   │   └── RewardRepository.cs
│   │   ├── Services/
│   │   │   └── RewardService.cs
│   │   └── Controllers/
│   │       └── RewardsController.cs
│
│   ├── AgentService/
│   │   ├── AgentService.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── Dockerfile
│   │   ├── Models/
│   │   │   └── Dealer.cs
│   │   ├── DTOs/
│   │   │   └── DealerDtos.cs
│   │   ├── Data/
│   │   │   └── AgentDbContext.cs
│   │   ├── Repositories/
│   │   │   └── DealerRepository.cs
│   │   ├── Services/
│   │   │   └── AgentService.cs
│   │   └── Controllers/
│   │       └── AgentsController.cs
│
│   ├── NotificationService/
│   │   ├── NotificationService.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── Dockerfile
│   │   ├── Models/
│   │   │   └── Notification.cs
│   │   ├── DTOs/
│   │   │   └── NotificationDtos.cs
│   │   ├── Data/
│   │   │   └── NotificationDbContext.cs
│   │   ├── Repositories/
│   │   │   └── NotificationRepository.cs
│   │   ├── Services/
│   │   │   └── NotificationService.cs
│   │   └── Controllers/
│   │       └── NotificationsController.cs
│
│   └── AdminService/
│       ├── AdminService.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       ├── Dockerfile
│       ├── DTOs/
│       │   └── AdminDtos.cs
│       ├── Services/
│       │   └── AdminService.cs
│       └── Controllers/
│           └── AdminController.cs

└── ApiGateway/
    ├── ApiGateway.csproj
    ├── Program.cs
    ├── appsettings.json
    ├── ocelot.json
    └── Dockerfile
```

## 📊 File Statistics

```
Total Directories: 42
Total Files: 105+

By Category:
├── Configuration Files: 15 (appsettings.json, Dockerfile, etc.)
├── Source Code Files (.cs): 95+
├── Documentation: 6
├── Docker: 12 (Dockerfiles + docker-compose.yml)
├── Other: 5 (.gitignore, etc.)

By Type:
├── .csproj files: 12
├── .cs files: 95+
├── .json files: 15
├── .md files: 6
├── Dockerfiles: 12
└── docker-compose.yml: 1
```

## 🗂️ Code Organization

```
Layer Structure (per service):
├── Controllers (API endpoints)
├── Services (Business logic)
├── Repositories (Data access)
├── Data (DbContext)
├── Models (Entity models)
└── DTOs (Data transfer objects)

Shared Resources:
├── Models (Base classes, enums)
├── Events (Integration events)
├── Security (JWT handling)
├── Configuration (Settings)
└── RabbitMQ (Event pub/sub)
```

## 📝 Configuration Files

```
appsettings.json (per service):
├── Logging
├── ConnectionStrings
├── JwtSettings
└── RabbitMqSettings (for event-driven services)

docker-compose.yml:
├── Services (12 containers)
├── Volumes (data persistence)
├── Networks (service communication)
└── Environment variables

ocelot.json (API Gateway):
├── Routes (10 service routes)
└── GlobalConfiguration
```

## 🔌 Database Files

```
Per Service Database Context:
├── IdentityDbContext.cs
├── FlightDbContext.cs
├── BookingDbContext.cs
├── PaymentDbContext.cs
├── CheckInDbContext.cs
├── BaggageDbContext.cs
├── RewardDbContext.cs
├── AgentDbContext.cs
└── NotificationDbContext.cs

(No separate context for AdminService - read-only aggregation)
```

## 🚀 Docker Files

```
Dockerfiles (per service):
├── Services/IdentityService/Dockerfile
├── Services/FlightService/Dockerfile
├── Services/BookingService/Dockerfile
├── Services/PaymentService/Dockerfile
├── Services/CheckInService/Dockerfile
├── Services/BaggageService/Dockerfile
├── Services/RewardService/Dockerfile
├── Services/AgentService/Dockerfile
├── Services/NotificationService/Dockerfile
├── Services/AdminService/Dockerfile
└── ApiGateway/Dockerfile

Docker Compose:
└── docker-compose.yml (Orchestration for all services)
```


## 🔐 Security Files

```
Authentication & Authorization:
├── Shared/Security/JwtTokenService.cs
├── Configuration files with JWT settings
└── Authorization attributes in controllers
```

## 📡 Event Infrastructure

```
RabbitMQ Integration:
├── Shared/RabbitMQ/EventPublisher.cs
├── Shared/Events/IntegrationEvents.cs
└── Event handlers in services
```

## 💾 Data Models

```
Entities (10 total):
├── User (IdentityService)
├── Flight (FlightService)
├── Booking (BookingService)
├── Payment (PaymentService)
├── CheckIn (CheckInService)
├── Baggage (BaggageService)
├── Reward (RewardService)
├── Dealer (AgentService)
├── DealerBooking (AgentService)
└── Notification (NotificationService)
```

## 📦 NuGet Packages

```
Common Packages (all services):
├── Microsoft.EntityFrameworkCore.SqlServer
├── Swashbuckle.AspNetCore (Swagger)
├── System.IdentityModel.Tokens.Jwt
├── Microsoft.AspNetCore.Authentication.JwtBearer

Service-Specific:
├── RabbitMQ.Client (Messaging services)
├── QRCoder (CheckInService)
├── BCrypt.Net-Next (IdentityService)
└── HttpClientFactory (AdminService)

API Gateway:
└── Ocelot
```

## 🎯 Project Entry Points

```
API Gateway:
└── http://localhost:5000

Individual Services (for development):
├── Identity: http://localhost:5001
├── Flight: http://localhost:5002
├── Booking: http://localhost:5003
├── Payment: http://localhost:5004
├── CheckIn: http://localhost:5005
├── Baggage: http://localhost:5006
├── Reward: http://localhost:5007
├── Agent: http://localhost:5008
├── Notification: http://localhost:5009
└── Admin: http://localhost:5010

RabbitMQ Management:
└── http://localhost:15672

SQL Server:
└── localhost:1433
```