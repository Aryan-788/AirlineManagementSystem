export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  role: number;
}

export interface AuthResponse {
  userId: number;
  email: string;
  name: string;
  role: string;
  token: string;
}

export interface UserProfile {
  id: number;
  name: string;
  email: string;
  role: string;
  createdAt: string;
}

export interface Flight {
  id: number;
  flightNumber: string;
  source: string;
  destination: string;
  departureTime: string;
  arrivalTime: string;
  gate: string;
  aircraft: string;
  status: string;
  totalSeats: number;
  availableSeats: number;
  economySeats: number;
  businessSeats: number;
  firstSeats: number;
  economyPrice: number;
  businessPrice: number;
  firstClassPrice: number;
}

export interface FlightSchedule {
  id: number;
  flightId: number;
  flightNumber: string;
  source: string;
  destination: string;
  aircraft: string;
  departureTime: string;
  arrivalTime: string;
  gate: string;
  status: string;
  totalSeats: number;
  availableSeats: number;
  economySeats: number;
  businessSeats: number;
  firstSeats: number;
  economyPrice: number;
  businessPrice: number;
  firstClassPrice: number;
  createdAt: string;
}

export interface CreateFlightRequest {
  flightNumber: string;
  source: string;
  destination: string;
  departureTime: string;
  arrivalTime: string;
  aircraft: string;
  totalSeats: number;
  economySeats: number;
  businessSeats: number;
  firstSeats: number;
  economyPrice?: number;
  businessPrice?: number;
  firstClassPrice?: number;
}

export interface CreateScheduleRequest {
  flightId: number;
  departureTime: string;
  arrivalTime: string;
  gate?: string;
  economySeats?: number;
  businessSeats?: number;
  firstSeats?: number;
  economyPrice?: number;
  businessPrice?: number;
  firstClassPrice?: number;
}

export interface UpdateScheduleRequest {
  departureTime?: string;
  arrivalTime?: string;
  gate?: string;
  status?: string;
  economySeats?: number;
  businessSeats?: number;
  firstSeats?: number;
  economyPrice?: number;
  businessPrice?: number;
  firstClassPrice?: number;
}

export interface Booking {
  id: number;
  userId: number;
  flightId: number;
  scheduleId?: number;
  seatClass: string;
  baggageWeight: number;
  pnr: string;
  status: string;
  paymentStatus: string;
  totalPassengers: number;
  confirmedPassengers: number;
  cancelledPassengers: number;
  createdAt: string;
  totalAmount: number;
}

export interface CreateBookingRequest {
  userId: number;
  flightId: number;
  scheduleId?: number;
  seatClass: string;
  baggageWeight: number;
  passengerCount: number;
  totalAmount: number;
}

export interface SchedulePassenger {
  id: number;
  name: string;
  age: number;
  gender: string;
  status: string;
  seat: string;
}

export interface ScheduleBooking {
  id: number;
  pnr: string;
  userId: number;
  seatClass: string;
  status: string;
  paymentStatus: string;
  passengers: SchedulePassenger[];
  totalAmount: number;
}

export interface Passenger {
  id: number;
  name: string;
  age: number;
  gender: string;
  aadharCardNo: string;
  status: string;
  seatNumber?: string;
  cancelledAt: string | null;
  cancellationReason: string | null;
  createdAt: string;
}

export interface CreatePassengerRequest {
  name: string;
  age: number;
  gender: string;
  aadharCardNo: string;
  seatNumber?: string;
}

export interface Payment {
  id: number;
  bookingId: number;
  amount: number;
  status: string;
  paymentMethod: string;
  createdAt: string;
}

export interface ProcessPaymentRequest {
  bookingId: number;
  amount: number;
  paymentMethod: string;
  userId: number;
}

export interface VerifyPaymentRequest {
  bookingId: number;
  razorpayOrderId: string;
  razorpayPaymentId: string;
  razorpaySignature: string;
  amount: number;
  userId: number;
}

export interface CheckIn {
  id: number;
  bookingId: number;
  seatNumber: string;
  gate: string;
  boardingPass: string;
  checkInTime: string;
}

export interface CheckInRequest {
  bookingId: number;
  userId: number;
  seatNumber?: string;
}

export interface BoardingPass {
  passengerName: string;
  flightNumber: string;
  gate: string;
  seatNumber: string;
  qrCode: string;
  departureTime: string;
}

export interface Baggage {
  id: number;
  bookingId: number;
  weight: number;
  passengerName: string;
  flightNumber: string;
  status: string;
  isDelivered: boolean;
  trackingNumber: string;
}

export interface Reward {
  id: number;
  userId: number;
  points: number;
  transactionType: string;
  createdAt: string;
}

export interface RewardBalance {
  userId: number;
  totalPoints: number;
}

export interface Dealer {
  id: number;
  dealerName: string;
  dealerEmail: string;
  allocatedSeats: number;
  usedSeats: number;
  availableSeats: number;
  commissionRate: number;
  isActive: boolean;
}

export interface DealerBooking {
  id: number;
  dealerId: number;
  bookingId: number;
  flightId: number;
  commission: number;
}

export interface CommissionReport {
  dealerId: number;
  dealerName: string;
  totalBookings: number;
  totalCommission: number;
}

export interface Notification {
  id: number;
  userId: number;
  email: string;
  subject: string;
  message: string;
  notificationType: string;
  isSent: boolean;
  isRead: boolean;
  createdAt: string;
}

export interface AdminDashboard {
  totalBookings: number;
  totalRevenue: number;
  activeFlights: number;
  totalUsers: number;
}

export interface RevenueReport {
  date: string;
  revenue: number;
  bookingCount: number;
}

export interface BaggageSummary {
  totalBags: number;
  deliveredCount: number;
  inTransitCount: number;
  checkedCount: number;
  totalWeight: number;
}

export interface CheckInSummary {
  totalCheckIns: number;
  todayCheckIns: number;
}

export interface DealerPerformance {
  allocatedSeats: number;
  usedSeats: number;
  availableSeats: number;
  totalCommission: number;
  totalBookings: number;
  commissionRate: number;
}

export interface UpdateProfileRequest {
  name: string;
  email: string;
}

export interface RefundDetails {
  id: number;
  bookingId: number;
  pnr: string;
  passengerId?: number;
  passengerName: string;
  passengerAadhar: string;
  cancellationReason: string;
  refundAmount: number;
  refundPercentage: number;
  refundStatus: string;
  cancellationTime: string;
  departureTime: string;
}
