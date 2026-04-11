# Airline Management System - Complete API Documentation

## Table of Contents
1. [Authentication](#authentication)
2. [Identity Service](#identity-service)
3. [Flight Service](#flight-service)
4. [Booking Service](#booking-service)
5. [Payment Service](#payment-service)
6. [CheckIn Service](#checkin-service)
7. [Baggage Service](#baggage-service)
8. [Reward Service](#reward-service)
9. [Agent Service](#agent-service)
10. [Notification Service](#notification-service)
11. [Admin Service](#admin-service)

## Authentication

All requests to protected endpoints must include a JWT token in the Authorization header:

```
Authorization: Bearer {token}
```

### Register User
**Endpoint**: `POST /identity/auth/register`

**Request**:
```json
{
  "name": "John Doe",
  "email": "john@example.com",
  "password": "SecurePassword123!",
  "role": "Passenger"
}
```

**Roles**: `Admin`, `Passenger`, `Dealer`, `GroundStaff`

**Response**:
```json
{
  "userId": 1,
  "email": "john@example.com",
  "name": "John Doe",
  "role": "Passenger",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### Login
**Endpoint**: `POST /identity/auth/login`

**Request**:
```json
{
  "email": "john@example.com",
  "password": "SecurePassword123!"
}
```

**Response**:
```json
{
  "userId": 1,
  "email": "john@example.com",
  "name": "John Doe",
  "role": "Passenger",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

---

## Identity Service

### Get User Profile
**Endpoint**: `GET /identity/auth/user/{userId}`  
**Auth**: Required (Any Role)

**Response**:
```json
{
  "id": 1,
  "name": "John Doe",
  "email": "john@example.com",
  "role": "Passenger",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

---

## Flight Service

### Create Flight
**Endpoint**: `POST /flights`  
**Auth**: Required (Admin Only)

**Request**:
```json
{
  "flightNumber": "AA001",
  "source": "NYC",
  "destination": "LAX",
  "departureTime": "2024-12-25T10:00:00Z",
  "arrivalTime": "2024-12-25T13:00:00Z",
  "aircraft": "Boeing 747",
  "totalSeats": 300,
  "economySeats": 250,
  "businessSeats": 40,
  "firstSeats": 10
}
```

**Response**:
```json
{
  "id": 1,
  "flightNumber": "AA001",
  "source": "NYC",
  "destination": "LAX",
  "departureTime": "2024-12-25T10:00:00Z",
  "arrivalTime": "2024-12-25T13:00:00Z",
  "gate": "",
  "aircraft": "Boeing 747",
  "status": "Scheduled",
  "totalSeats": 300,
  "availableSeats": 300,
  "economySeats": 250,
  "businessSeats": 40,
  "firstSeats": 10
}
```

### Search Flights
**Endpoint**: `GET /flights/search?source=NYC&destination=LAX&departureDate=2024-12-25`  
**Auth**: Not Required

**Response**:
```json
[
  {
    "id": 1,
    "flightNumber": "AA001",
    "source": "NYC",
    "destination": "LAX",
    "departureTime": "2024-12-25T10:00:00Z",
    "status": "Scheduled",
    "availableSeats": 300
  }
]
```

### Get All Flights
**Endpoint**: `GET /flights`  
**Auth**: Not Required

### Get Flight Details
**Endpoint**: `GET /flights/{id}`  
**Auth**: Not Required

### Update Flight
**Endpoint**: `PUT /flights/{id}`  
**Auth**: Required (Admin Only)

**Request**:
```json
{
  "departureTime": "2024-12-25T11:00:00Z",
  "gate": "A10",
  "aircraft": "Boeing 777"
}
```

### Delete Flight
**Endpoint**: `DELETE /flights/{id}`  
**Auth**: Required (Admin Only)

### Delay Flight
**Endpoint**: `POST /flights/{id}/delay`  
**Auth**: Required (Admin Only)

**Request**: `"2024-12-25T12:00:00Z"` (new departure time as raw value)

### Cancel Flight
**Endpoint**: `POST /flights/{id}/cancel`  
**Auth**: Required (Admin Only)

### Assign Gate
**Endpoint**: `POST /flights/{id}/assign-gate`  
**Auth**: Required (Admin Only)

**Request**: `"A10"` (gate number as raw value)

### Assign Aircraft
**Endpoint**: `POST /flights/{id}/assign-aircraft`  
**Auth**: Required (Admin Only)

**Request**: `"Boeing 787"` (aircraft name as raw value)

### Assign Crew
**Endpoint**: `POST /flights/{id}/assign-crew`  
**Auth**: Required (Admin Only)

**Request**: `"Crew ID: C001"` (crew info as raw value)

---

## Booking Service

### Create Booking
**Endpoint**: `POST /bookings`  
**Auth**: Required (Passenger, Dealer)

**Request**:
```json
{
  "userId": 1,
  "flightId": 1,
  "seatClass": "Business",
  "baggageWeight": 25,
  "passengerName": "John Doe",
  "passengerEmail": "john@example.com",
  "passengerPhone": "1234567890"
}
```

**Response**:
```json
{
  "id": 1,
  "userId": 1,
  "flightId": 1,
  "seatClass": "Business",
  "baggageWeight": 25,
  "pnr": "ABC123",
  "status": "Pending",
  "paymentStatus": "Pending",
  "passengerName": "John Doe",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

### Get Booking
**Endpoint**: `GET /bookings/{id}`  
**Auth**: Required (Passenger, Dealer)

### Get Booking History
**Endpoint**: `GET /bookings/history/{userId}`  
**Auth**: Required (Passenger, Dealer)

**Response**:
```json
[
  {
    "id": 1,
    "flightId": 1,
    "pnr": "ABC123",
    "status": "Confirmed",
    "createdAt": "2024-01-15T10:30:00Z"
  }
]
```

### Cancel Booking
**Endpoint**: `POST /bookings/{id}/cancel`  
**Auth**: Required (Passenger, Dealer)

---

### Passenger Management

### Add Passengers
**Endpoint**: `POST /bookings/{bookingId}/passengers`  
**Auth**: Required (Passenger, Dealer)

**Request**:
```json
[
  {
    "name": "Jane Doe",
    "age": 28,
    "gender": "Female",
    "seatNumber": "15C",
    "passportNumber": "P1234567"
  }
]
```

### Get Booking Passengers
**Endpoint**: `GET /bookings/{bookingId}/passengers`  
**Auth**: Required (Passenger, Dealer)

---

### Advanced Cancellation & Refunds

### Cancel Single Passenger
**Endpoint**: `POST /bookings/passengers/{passengerId}/cancel`  
**Auth**: Required (Passenger, Dealer)

**Request**:
```json
{
  "reason": "Health issues",
  "cancelConfirmation": true
}
```

### Cancel Multiple Passengers
**Endpoint**: `POST /bookings/{bookingId}/passengers/cancel`  
**Auth**: Required (Passenger, Dealer)

**Request**:
```json
{
  "passengerIds": [1, 2],
  "reason": "Plan change"
}
```

### Get Refund Reports (Admin)
**Endpoint**: `GET /bookings/refunds/all`  
**Auth**: Required (Admin Only)

**Response**:
```json
[
  {
    "bookingId": 1,
    "passengerName": "John Doe",
    "refundAmount": 450.00,
    "refundPercentage": 90.0,
    "status": "RefundPending",
    "cancellationTime": "2024-01-15T10:30:00Z"
  }
]
```

---

## Cancellation & Refund Policy

Refunds are calculated based on the time remaining before the flight departure:

| Time Before Departure | Refund Percentage |
|-----------------------|-------------------|
| More than 48 hours    | 90% Refund        |
| 24 to 48 hours        | 50% Refund        |
| Less than 24 hours    | 0% Refund         |

---

## Payment Service

### Process Payment
**Endpoint**: `POST /payments/process`  
**Auth**: Required (Passenger, Dealer)

**Request**:
```json
{
  "bookingId": 1,
  "amount": 500,
  "paymentMethod": "CreditCard"
}
```

**Response**:
```json
{
  "id": 1,
  "bookingId": 1,
  "amount": 500,
  "status": "Success",
  "paymentMethod": "CreditCard",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

### Get Payment
**Endpoint**: `GET /payments/{id}`  
**Auth**: Required (Any Role)

### Refund Payment
**Endpoint**: `POST /payments/{id}/refund`  
**Auth**: Required (Admin Only)

**Response**:
```json
{
  "id": 1,
  "bookingId": 1,
  "amount": 500,
  "status": "Refunded",
  "paymentMethod": "CreditCard",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

---

## CheckIn Service

### Online Check-In
**Endpoint**: `POST /checkins/online?passengerName=John Doe&flightNumber=AA001&flightId=1&departureTime=2024-12-25T10:00:00Z`  
**Auth**: Required (Passenger)

**Request**:
```json
{
  "bookingId": 1,
  "userId": 1
}
```

**Response**:
```json
{
  "id": 1,
  "bookingId": 1,
  "seatNumber": "12A",
  "gate": "A10",
  "boardingPass": "John Doe|AA001|12A",
  "checkInTime": "2024-12-25T08:00:00Z"
}
```

### Get Check-In Details
**Endpoint**: `GET /checkins/{id}`  
**Auth**: Required (Passenger)

### Generate Boarding Pass
**Endpoint**: `GET /checkins/{id}/boarding-pass`  
**Auth**: Required (Passenger)

**Response**:
```json
{
  "passengerName": "John Doe",
  "flightNumber": "AA001",
  "gate": "A10",
  "seatNumber": "12A",
  "qrCode": "base64_encoded_qr_code",
  "departureTime": "2024-12-25T10:00:00Z"
}
```

---

## Baggage Service

### Add Baggage
**Endpoint**: `POST /baggages`  
**Auth**: Required (GroundStaff)

**Request**:
```json
{
  "bookingId": 1,
  "weight": 25
}
```

**Response**:
```json
{
  "id": 1,
  "bookingId": 1,
  "weight": 25,
  "status": "Checked",
  "isDelivered": false,
  "trackingNumber": "BAG-20240115-A1B2C3D4"
}
```

### Get Baggage Details
**Endpoint**: `GET /baggages/{id}`  
**Auth**: Required (GroundStaff, Passenger)

### Update Baggage Status
**Endpoint**: `PUT /baggages/{id}/status`  
**Auth**: Required (GroundStaff)

**Request**:
```json
{
  "status": "Loaded"
}
```

**Status Values**: `Checked`, `Loaded`, `InTransit`, `Delivered`, `Lost`

### Mark Baggage Delivered
**Endpoint**: `POST /baggages/{id}/deliver`  
**Auth**: Required (GroundStaff)

### Track Baggage
**Endpoint**: `GET /baggages/track/{trackingNumber}`  
**Auth**: Required (Passenger, GroundStaff)

### Get Baggage by Booking
**Endpoint**: `GET /baggages/booking/{bookingId}`  
**Auth**: Required (GroundStaff, Passenger)

---

## Reward Service

### Earn Points
**Endpoint**: `POST /rewards/earn?transactionType=Booking&bookingId=1`  
**Auth**: Required (Passenger, Dealer)

**Request**:
```json
{
  "userId": 1,
  "points": 100,
  "transactionType": "Booking"
}
```

**Response**:
```json
{
  "id": 1,
  "userId": 1,
  "points": 100,
  "transactionType": "Booking",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

### Get Reward Balance
**Endpoint**: `GET /rewards/{userId}/balance`  
**Auth**: Required (Passenger)

**Response**:
```json
{
  "userId": 1,
  "totalPoints": 1500
}
```

### Get Reward History
**Endpoint**: `GET /rewards/{userId}/history`  
**Auth**: Required (Passenger)

**Response**:
```json
[
  {
    "id": 1,
    "userId": 1,
    "points": 100,
    "transactionType": "Booking",
    "createdAt": "2024-01-15T10:30:00Z"
  }
]
```

### Redeem Points
**Endpoint**: `POST /rewards/redeem`  
**Auth**: Required (Passenger)

**Request**:
```json
{
  "userId": 1,
  "points": 500
}
```

---

## Agent Service

### Create Dealer
**Endpoint**: `POST /agents/dealer`  
**Auth**: Required (Admin Only)

**Request**:
```json
{
  "dealerName": "Travel Agency XYZ",
  "dealerEmail": "dealer@xyz.com",
  "allocatedSeats": 100,
  "commissionRate": 5
}
```

**Response**:
```json
{
  "id": 1,
  "dealerName": "Travel Agency XYZ",
  "dealerEmail": "dealer@xyz.com",
  "allocatedSeats": 100,
  "usedSeats": 0,
  "availableSeats": 100,
  "commissionRate": 5,
  "isActive": true
}
```

### Get Dealer
**Endpoint**: `GET /agents/dealer/{id}`  
**Auth**: Required (Any Role)

### Get All Dealers
**Endpoint**: `GET /agents/dealers`  
**Auth**: Required (Admin Only)

### Allocate Seats
**Endpoint**: `POST /agents/dealer/{dealerId}/allocate-seats?seats=50`  
**Auth**: Required (Admin Only)

### Record Dealer Booking
**Endpoint**: `POST /agents/booking/record?dealerId=1&bookingId=1&flightId=1&bookingAmount=500`  
**Auth**: Required (Dealer, Admin)

**Response**:
```json
{
  "id": 1,
  "dealerId": 1,
  "bookingId": 1,
  "flightId": 1,
  "commission": 25
}
```

### Get Commission Report
**Endpoint**: `GET /agents/commission-report`  
**Auth**: Required (Admin Only)

**Response**:
```json
[
  {
    "dealerId": 1,
    "dealerName": "Travel Agency XYZ",
    "totalBookings": 50,
    "totalCommission": 2500
  }
]
```

---

## Notification Service

### Get Notification
**Endpoint**: `GET /notifications/{id}`  
**Auth**: Required (Any Role)

**Response**:
```json
{
  "id": 1,
  "userId": 1,
  "email": "john@example.com",
  "subject": "Booking Confirmation",
  "message": "Your booking has been created successfully.",
  "notificationType": "BookingConfirmation",
  "isSent": false,
  "createdAt": "2024-01-15T10:30:00Z"
}
```

### Get User Notifications
**Endpoint**: `GET /notifications/user/{userId}`  
**Auth**: Required (Any Role)

---

## Admin Service

### Get Dashboard
**Endpoint**: `GET /admin/dashboard`  
**Auth**: Required (Admin Only)

**Response**:
```json
{
  "totalBookings": 1500,
  "totalRevenue": 750000,
  "activeFlights": 25,
  "totalUsers": 500
}
```

### Get Booking Report
**Endpoint**: `GET /admin/booking-report?startDate=2024-01-01&endDate=2024-01-31`  
**Auth**: Required (Admin Only)

**Response**:
```json
[
  {
    "bookingId": 1,
    "userId": 1,
    "flightId": 1,
    "status": "Confirmed",
    "createdAt": "2024-01-15T10:30:00Z"
  }
]
```

### Get Revenue Report
**Endpoint**: `GET /admin/revenue-report?startDate=2024-01-01&endDate=2024-01-31`  
**Auth**: Required (Admin Only)

**Response**:
```json
[
  {
    "date": "2024-01-15",
    "revenue": 25000,
    "bookingCount": 50
  }
]
```

---

## Error Handling

All endpoints follow a consistent error response format:

**Error Response**:
```json
{
  "message": "Error description"
}
```

**Common HTTP Status Codes**:
- `200 OK`: Request successful
- `201 Created`: Resource created
- `204 No Content`: Request successful, no content
- `400 Bad Request`: Invalid request parameters
- `401 Unauthorized`: Missing or invalid authentication
- `403 Forbidden`: Insufficient permissions
- `404 Not Found`: Resource not found
- `500 Internal Server Error`: Server error

---

## Event-Driven Communication

The system publishes the following events to RabbitMQ:

### BookingCreatedEvent
Triggered when a booking is created
```json
{
  "bookingId": 1,
  "userId": 1,
  "flightId": 1,
  "seatClass": "Business",
  "amount": 500,
  "createdAt": "2024-01-15T10:30:00Z"
}
```

### BookingCancelledEvent
Triggered when a full booking or passenger is cancelled
```json
{
  "bookingId": 1,
  "userId": 1,
  "flightId": 1,
  "refundAmount": 450.00,
  "cancelledAt": "2024-01-15T10:30:00Z"
}
```

### RefundProcessedEvent
Triggered when a refund is calculated and recorded
```json
{
  "bookingId": 1,
  "passengerId": 1,
  "userId": 1,
  "refundAmount": 450.00,
  "refundPercentage": 90.0,
  "processedAt": "2024-01-15T10:30:00Z"
}
```

### RewardEarnedEvent
Triggered when a booking is successfully paid
```json
{
  "userId": 1,
  "points": 100,
  "bookingId": 1,
  "earnedAt": "2024-01-15T10:30:00Z"
}
```

### CheckInCompletedEvent
Triggered when check-in is completed
```json
{
  "bookingId": 1,
  "userId": 1,
  "flightId": 1,
  "boardingPass": "John Doe|AA001|12A",
  "checkedInAt": "2024-01-15T10:30:00Z"
}
```

---

## Rate Limiting & Performance

- JWT token expiration: 60 minutes (configurable)
- Database connection pooling enabled
- RabbitMQ auto-recovery enabled
- CORS enabled for frontend integration

---

## Support

For API issues or questions, check:
1. Service-specific Swagger documentation
2. Docker logs: `docker logs <service-name>`
3. RabbitMQ management console for message queues
4. SQL Server for data verification

---

**Last Updated**: December 2024  
**System Version**: 1.0.0  
**Framework**: .NET 10
