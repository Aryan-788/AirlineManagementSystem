import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TopNavbarComponent } from '../../../shared/components/top-navbar.component';
import { SideNavbarComponent } from '../../../shared/components/side-navbar.component';
import { AuthService } from '../../../core/services/auth.service';
import { BookingService } from '../../../core/services/booking.service';
import { RewardService } from '../../../core/services/reward.service';
import { NotificationService } from '../../../core/services/notification.service';
import { FlightService } from '../../../core/services/flight.service';
import { Booking, RewardBalance, Notification, Flight } from '../../../core/models/api.models';

@Component({
  selector: 'app-passenger-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, TopNavbarComponent, SideNavbarComponent],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class PassengerDashboardComponent implements OnInit {
  bookings = signal<Booking[]>([]);
  rewardBalance = signal<RewardBalance>({ userId: 0, totalPoints: 0 });
  notifications = signal<Notification[]>([]);
  loading = signal(true);
  nextDeparture = signal<Booking | null>(null);

  // Modal for View Details
  showModal = signal(false);
  selectedBooking = signal<Booking | null>(null);
  selectedFlight = signal<Flight | null>(null);
  passengersOfBooking = signal<any[]>([]);
  modalLoading = signal(false);

  sideNavItems = [
    { icon: 'dashboard', label: 'Dashboard', route: '/passenger/dashboard' },
    { icon: 'travel_explore', label: 'Search Flights', route: '/passenger/search' },
    { icon: 'confirmation_number', label: 'My Bookings', route: '/passenger/my-bookings' },
    { icon: 'stars', label: 'Rewards', route: '/passenger/rewards' },
  ];

  constructor(
    public authService: AuthService,
    private bookingService: BookingService,
    private rewardService: RewardService,
    private notificationService: NotificationService,
    private flightService: FlightService
  ) {}

  ngOnInit() {
    const userId = this.authService.userId();
    this.bookingService.getBookingHistory(userId).subscribe({
      next: (data) => { 
        this.bookings.set(data); 
        this.loading.set(false); 
        
        // Find next confirmed departure
        const confirmed = data
          .filter(b => b.status?.toLowerCase() === 'confirmed')
          .sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
        
        if (confirmed.length > 0) {
          this.nextDeparture.set(confirmed[0]);
        }
      },
      error: () => this.loading.set(false)
    });
    this.rewardService.getBalance(userId).subscribe({
      next: (data) => this.rewardBalance.set(data),
      error: () => {}
    });
    this.notificationService.getUserNotifications(userId).subscribe({
      next: (data) => this.notifications.set(data),
      error: () => {}
    });
  }

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'confirmed': return 'status-confirmed';
      case 'pending': return 'status-pending';
      case 'cancelled': return 'status-cancelled';
      default: return 'status-default';
    }
  }

  viewBooking(booking: Booking) {
    this.selectedBooking.set(booking);
    this.showModal.set(true);
    this.modalLoading.set(true);
    this.passengersOfBooking.set([]);
    this.selectedFlight.set(null);
    
    this.bookingService.getPassengers(booking.id).subscribe({
      next: (data) => {
        this.passengersOfBooking.set(data);
        if (this.selectedFlight()) this.modalLoading.set(false);
      },
      error: () => this.modalLoading.set(false)
    });

    this.flightService.getFlightById(booking.flightId).subscribe({
      next: (flight) => {
        this.selectedFlight.set(flight);
        if (this.passengersOfBooking().length > 0) this.modalLoading.set(false);
      },
      error: () => this.modalLoading.set(false)
    });
  }

  getDuration(departure: string, arrival: string): string {
    if (!departure || !arrival) return 'N/A';
    const start = new Date(departure);
    const end = new Date(arrival);
    const diffMs = end.getTime() - start.getTime();
    const diffHrs = Math.floor(diffMs / (1000 * 60 * 60));
    const diffMins = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60));
    return `${diffHrs}h ${diffMins}m`;
  }

  closeModal() {
    this.showModal.set(false);
    this.selectedBooking.set(null);
    this.selectedFlight.set(null);
  }
}
