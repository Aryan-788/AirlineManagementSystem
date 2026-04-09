# Airline Management System - Production Ready Microservices

A complete **production-ready Airline Management System** built with **.NET 10 Microservices Architecture**, featuring event-driven design, RabbitMQ messaging, JWT authentication, Docker containerization, and an Ocelot API Gateway.

## 🎯 System Overview

The system is designed with 4 user roles:
- **Admin**: System management, flight scheduling, reporting
- **Passenger**: Flight booking, check-in, reward management
- **Dealer/Agent**: Seat allocation, commission tracking, booking management
- **Ground Operations Staff**: Baggage tracking, check-in support

## 🏗️ Architecture

### Microservices (11 Services)
1. **Identity Service** - User authentication & JWT generation
2. **Flight Service** - Flight management & scheduling
3. **Booking Service** - Booking creation & management
4. **Payment Service** - Payment processing
5. **CheckIn Service** - Online check-in & boarding passes
6. **Baggage Service** - Baggage tracking & management
7. **Reward Service** - Loyalty points management
8. **Agent Service** - Dealer/Agent management
9. **Notification Service** - Event-driven notifications
10. **Admin Service** - Dashboard & reporting
11. **API Gateway** - Ocelot-based request routing

## 🔧 Tech Stack

- **Runtime**: .NET 10
- **Database**: SQL Server (containerized)
- **Message Queue**: RabbitMQ
- **API Gateway**: Ocelot
- **Authentication**: JWT with Role-Based Authorization
- **ORM**: Entity Framework Core
- **Documentation**: Swagger/OpenAPI
- **Containerization**: Docker & Docker Compose

## 📋 Features

### Core Features
✅ Multi-tenancy ready with role-based access control  
✅ Event-driven microservices architecture  
✅ RabbitMQ-based asynchronous communication  
✅ JWT authentication across all services  
✅ Full CRUD operations for all entities  
✅ Comprehensive error handling  
✅ Swagger documentation on each service  

### Flight Management
✅ Create, update, delete flights  
✅ Schedule flights with seat allocation  
✅ Delay and cancel flights  
✅ Assign gates, aircraft, and crew  
✅ Real-time seat availability  

### Booking System
✅ Search flights by route and date  
✅ Create bookings with seat class selection  
✅ Automatic PNR generation  
✅ Booking history and cancellation  
✅ Passenger information management  

### Payment Processing
✅ Process payments for bookings  
✅ Payment status tracking  
✅ Refund processing  
✅ Multiple payment methods support  

### Check-In System
✅ Online check-in  
✅ Automatic seat assignment  
✅ Boarding pass generation  
✅ QR code generation for boarding passes  

### Baggage Management
✅ Baggage tracking  
✅ Status updates (checked, loaded, delivered)  
✅ Weight tracking  
✅ Delivery confirmation  

### Reward System
✅ Earn points on bookings  
✅ Point redemption  
✅ Balance inquiry  
✅ Transaction history  

### Agent/Dealer Management
✅ Dealer registration  
✅ Seat allocation  
✅ Commission calculation  
✅ Dealer booking tracking  
✅ Commission reports  

### Notifications
✅ Booking confirmations  
✅ Payment notifications  
✅ Flight delay alerts  
✅ Check-in confirmations  
✅ Gate change notifications  

### Admin Dashboard
✅ Total bookings overview  
✅ Revenue tracking  
✅ Active flights monitoring  
✅ Dealer commission reports  
✅ Refund reports  

## 🚀 Getting Started

### Prerequisites
- Docker Desktop
- .NET 10 SDK (for local development)
- Visual Studio 2026 or higher

### Installation & Setup

#### Option 1: Docker Compose (Recommended)
```bash
# Navigate to project root
cd AirlineManagementSystem

# Start all services
docker-compose up -d

# Access the API Gateway
# http://localhost:5000

# RabbitMQ Management Console
# http://localhost:15672 (username: guest, password: guest)

# SQL Server
# Server: localhost,1433 (username: sa, password: YourPassword123!)
```

#### Option 2: Local Development
```bash
# Build the solution
dotnet build

# Run migrations for each service
# Identity Service
cd Services/IdentityService
dotnet ef database update

# Flight Service
cd ../FlightService
dotnet ef database update

# And so on for each service...

# Run each service individually
dotnet run
```

## 📡 API Endpoints

### API Gateway Routes (via Ocelot)
```
/identity/*          → Identity Service
/flights/*           → Flight Service
/bookings/*          → Booking Service
/payments/*          → Payment Service
/checkin/*           → CheckIn Service
/baggage/*           → Baggage Service
/reward/*            → Reward Service
/agent/*             → Agent Service
/notification/*      → Notification Service
/admin/*             → Admin Service
```

## 🔐 Authentication

All protected endpoints require JWT token in Authorization header:
```
Authorization: Bearer {token}
```

### Sample Login Request
```bash
POST http://localhost:5000/identity/auth/login
Content-Type: application/json

{
  "email": "admin@airline.com",
  "password": "password123"
}
```

### Response
```json
{
  "userId": 1,
  "email": "admin@airline.com",
  "name": "Admin User",
  "role": "Admin",
  "token": "eyJhbGc..."
}
```

## 🔄 Event Flow

The system uses RabbitMQ for asynchronous communication:

```
BookingCreated Event
    ↓
Payment Service + Reward Service + Notification Service

PaymentSuccess Event
    ↓
Booking Service (confirm booking) + Notification Service

FlightDelayed Event
    ↓
Notification Service (alert passengers)

CheckInCompleted Event
    ↓
Baggage Service + Notification Service

BaggageChecked Event
    ↓
Booking Service + Notification Service
```

## 📊 Database Schema

Each service has its own database:
- **IdentityDb**: User accounts and authentication
- **FlightDb**: Flight information and schedules
- **BookingDb**: Bookings and PNR information
- **PaymentDb**: Payment records and transactions
- **CheckInDb**: Check-in and boarding pass data
- **BaggageDb**: Baggage tracking information
- **RewardDb**: Reward points and transactions
- **AgentDb**: Dealer information and bookings
- **NotificationDb**: Notification history
- **AdminDb**: (Optional) Aggregated reports

## 🧪 API Testing

### Using Swagger UI
Each service exposes Swagger documentation:
- Identity: http://localhost:5001/swagger
- Flight: http://localhost:5002/swagger
- Booking: http://localhost:5003/swagger
- Payment: http://localhost:5004/swagger
- CheckIn: http://localhost:5005/swagger
- Baggage: http://localhost:5006/swagger
- Reward: http://localhost:5007/swagger
- Agent: http://localhost:5008/swagger
- Notification: http://localhost:5009/swagger
- Admin: http://localhost:5010/swagger

### Sample Requests

**Register User**
```bash
POST /identity/auth/register
{
  "name": "John Passenger",
  "email": "john@example.com",
  "password": "Secure123!",
  "role": "Passenger"
}
```

**Search Flights**
```bash
GET /flights/search?source=NYC&destination=LAX&departureDate=2024-12-25
```

**Create Booking**
```bash
POST /bookings
{
  "userId": 1,
  "flightId": 1,
  "seatClass": "Business",
  "baggageWeight": 25,
  "passengerName": "John Passenger",
  "passengerEmail": "john@example.com",
  "passengerPhone": "1234567890"
}
```

**Process Payment**
```bash
POST /payments/process
{
  "bookingId": 1,
  "amount": 500,
  "paymentMethod": "CreditCard"
}
```

## 🔑 Default Admin Account
- **Email**: admin@airline.com
- **Password**: Admin123!
- **Role**: Admin

(Create via registration endpoint)

## 📝 Project Structure

```
AirlineManagementSystem/
├── Shared/                          # Shared utilities
│   ├── Models/
│   ├── Events/
│   ├── Configuration/
│   ├── Security/
│   └── RabbitMQ/
├── Services/
│   ├── IdentityService/
│   ├── FlightService/
│   ├── BookingService/
│   ├── PaymentService/
│   ├── CheckInService/
│   ├── BaggageService/
│   ├── RewardService/
│   ├── AgentService/
│   ├── NotificationService/
│   └── AdminService/
├── ApiGateway/
├── docker-compose.yml
└── README.md
```

## 🛠️ Configuration

### JWT Settings (appsettings.json)
```json
{
  "JwtSettings": {
    "Key": "your-super-secret-key-that-is-at-least-32-characters-long",
    "Issuer": "AirlineIdentityService",
    "Audience": "AirlineManagementSystem",
    "ExpirationMinutes": 60
  }
}
```

### RabbitMQ Settings
```json
{
  "RabbitMqSettings": {
    "HostName": "rabbitmq",
    "UserName": "guest",
    "Password": "guest",
    "Port": 5672
  }
}
```

### Database Connection
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sqlserver;Database=FlightDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true;"
  }
}
```

## 🚦 Health Checks

Each service exposes health check endpoints:
```bash
GET /health
GET /ready
```

## 📈 Scaling & Deployment

The system is designed for horizontal scaling:
- Each service runs in its own container
- Shared RabbitMQ for inter-service communication
- Centralized SQL Server database
- Ocelot API Gateway for load balancing

### Production Deployment
For production deployment:
1. Update JWT keys and secrets
2. Configure production database
3. Set up log aggregation (ELK stack)
4. Configure monitoring (Prometheus)
5. Use orchestration platform (Kubernetes)

## 🐛 Troubleshooting

### Services not connecting
- Check Docker network: `docker network ls`
- Verify service names in docker-compose.yml
- Check logs: `docker logs <container-name>`

### Database connection issues
- Ensure SQL Server is running
- Verify connection strings match docker-compose environment variables
- Run migrations: `dotnet ef database update`

### RabbitMQ issues
- Access management console: http://localhost:15672
- Check queue bindings and exchange configuration
- Verify consumer subscriptions

## 📚 Additional Resources

- [.NET 10 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [Ocelot API Gateway](https://ocelot.readthedocs.io/)
- [RabbitMQ Tutorials](https://www.rabbitmq.com/getstarted.html)
- [Docker Documentation](https://docs.docker.com/)

## 🤝 Contributing

This is a complete system ready for production use. Feel free to extend with additional features:
- Add caching layer (Redis)
- Implement advanced logging (Serilog, ELK)
- Add API rate limiting
- Implement circuit breakers (Polly)
- Add unit and integration tests

## 📄 License

This project is provided as-is for educational and commercial use.

## ✨ Features Implemented

- ✅ 11 Microservices
- ✅ Event-driven architecture
- ✅ JWT Authentication
- ✅ Role-based authorization
- ✅ RabbitMQ integration
- ✅ API Gateway (Ocelot)
- ✅ Docker containerization
- ✅ Swagger documentation
- ✅ Complete business logic
- ✅ Production-ready code

---

**System Ready for Production Deployment** 🚀

For questions or support, refer to the documentation in each service folder.
