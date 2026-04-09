import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TopNavbarComponent } from '../../../shared/components/top-navbar.component';
import { SideNavbarComponent } from '../../../shared/components/side-navbar.component';
import { BookingService } from '../../../core/services/booking.service';
import { FlightService } from '../../../core/services/flight.service';
import { BaggageService } from '../../../core/services/baggage.service';
import { AuthService } from '../../../core/services/auth.service';
import { Booking, Flight, Baggage } from '../../../core/models/api.models';

@Component({
  selector: 'app-my-bookings',
  standalone: true,
  imports: [CommonModule, RouterModule, TopNavbarComponent, SideNavbarComponent],
  templateUrl: './my-bookings.html',
  styleUrl: './my-bookings.css'
})
export class MyBookingsComponent implements OnInit {
  bookings = signal<Booking[]>([]);
  loading = signal(true);
  activeFilter = signal<'all' | 'confirmed' | 'pending' | 'cancelled'>('all');
  
  // Pagination
  currentPage = signal(1);
  itemsPerPage = 10;
  
  // Modal for View Details
  showModal = signal(false);
  selectedBooking = signal<Booking | null>(null);
  selectedFlight = signal<Flight | null>(null);
  passengersOfBooking = signal<any[]>([]);
  baggagesOfBooking = signal<Baggage[]>([]);
  modalLoading = signal(false);

  sideNavItems = computed(() => {
    if (this.authService.userRole() === 'Dealer') {
      return [
        { icon: 'dashboard', label: 'Dashboard', route: '/dealer/dashboard' },
        { icon: 'travel_explore', label: 'Search Flights', route: '/dealer/search' },
        { icon: 'confirmation_number', label: 'My Bookings', route: '/dealer/my-bookings' },
      ];
    }
    return [
      { icon: 'dashboard', label: 'Dashboard', route: '/passenger/dashboard' },
      { icon: 'travel_explore', label: 'Search Flights', route: '/passenger/search' },
      { icon: 'confirmation_number', label: 'My Bookings', route: '/passenger/my-bookings' },
      { icon: 'stars', label: 'Rewards', route: '/passenger/rewards' },
    ];
  });

  constructor(
    public authService: AuthService,
    private bookingService: BookingService,
    private flightService: FlightService,
    private baggageService: BaggageService
  ) {}

  ngOnInit() {
    const userId = this.authService.userId();
    if (userId) {
      this.bookingService.getUserBookings(userId).subscribe({
        next: (data) => { this.bookings.set(data); this.loading.set(false); },
        error: () => this.loading.set(false)
      });
    }
  }

  filteredBookings = computed(() => {
    const filter = this.activeFilter();
    let filtered = this.bookings();
    if (filter !== 'all') {
      filtered = filtered.filter(b => b.status?.toLowerCase() === filter);
    }
    
    // Apply pagination
    const start = (this.currentPage() - 1) * this.itemsPerPage;
    return filtered.slice(start, start + this.itemsPerPage);
  });

  totalFilteredCount = computed(() => {
    const filter = this.activeFilter();
    if (filter === 'all') return this.bookings().length;
    return this.bookings().filter(b => b.status?.toLowerCase() === filter).length;
  });

  totalPages = computed(() => {
    return Math.ceil(this.totalFilteredCount() / this.itemsPerPage);
  });

  pages = computed(() => {
    const total = this.totalPages();
    return Array.from({ length: total }, (_, i) => i + 1);
  });

  setPage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
    }
  }

  changeFilter(filter: 'all' | 'confirmed' | 'pending' | 'cancelled') {
    this.activeFilter.set(filter);
    this.currentPage.set(1); // Reset to first page on filter change
  }

  upcomingCount = computed(() => {
    return this.bookings().filter(b => b.status?.toLowerCase() === 'confirmed').length;
  });
  pendingCount = computed(() => {
    return this.bookings().filter(b => b.status?.toLowerCase() === 'pending').length;
  });
  cancelledCount = computed(() => {
    return this.bookings().filter(b => b.status?.toLowerCase() === 'cancelled').length;
  });

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'confirmed': return 'st-confirmed';
      case 'pending': return 'st-pending';
      case 'cancelled': return 'st-cancelled';
      default: return 'st-default';
    }
  }

  cancelBooking(id: number) {
    if (!confirm('Are you sure you want to cancel this booking?')) return;
    this.bookingService.cancelBooking(id).subscribe({
      next: () => {
        this.bookings.update(bs => bs.map(b => b.id === id ? { ...b, status: 'Cancelled' } : b));
      }
    });
  }

  viewBooking(booking: Booking) {
    this.selectedBooking.set(booking);
    this.showModal.set(true);
    this.modalLoading.set(true);
    this.passengersOfBooking.set([]);
    this.selectedFlight.set(null);
    
    // Fetch passengers
    this.bookingService.getPassengers(booking.id).subscribe({
      next: (data) => {
        this.passengersOfBooking.set(data);
        // If we also finished fetching flight, stop loading
        if (this.selectedFlight()) this.modalLoading.set(false);
      },
      error: () => this.modalLoading.set(false)
    });

    // Fetch flight details
    this.flightService.getFlightById(booking.flightId).subscribe({
      next: (flight) => {
        this.selectedFlight.set(flight);
        // If we also finished fetching passengers, stop loading
        if (this.passengersOfBooking().length > 0) this.modalLoading.set(false);
      },
      error: () => this.modalLoading.set(false)
    });

    // Fetch baggage details
    this.baggageService.getBaggageByBooking(booking.id).subscribe({
      next: (bags) => this.baggagesOfBooking.set(bags),
      error: () => this.baggagesOfBooking.set([])
    });
  }

  cancelPassenger(passengerId: number) {
    if (!confirm('Are you sure you want to cancel this passenger?')) return;
    const reason = prompt('Please enter cancellation reason:');
    if (reason === null) return;
    
    this.bookingService.cancelPassenger(passengerId, reason || 'User requested cancel').subscribe({
      next: () => {
        this.passengersOfBooking.update(paxList => paxList.map(p => 
          p.id === passengerId ? { ...p, status: 'Cancelled', cancellationReason: reason || 'User requested cancel' } : p
        ));
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to cancel passenger');
      }
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
    this.baggagesOfBooking.set([]);
  }
}
