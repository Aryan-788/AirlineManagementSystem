import { AirlineTimePipe } from '../../../shared/pipes/airline-time.pipe';
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
  imports: [CommonModule, RouterModule, TopNavbarComponent, SideNavbarComponent, AirlineTimePipe],
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
  selectedPassengerIds = signal<number[]>([]);

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
      case 'refunded': return 'st-refunded';
      case 'partiallycancelled': return 'st-partiallycancelled';
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
    this.selectedPassengerIds.set([]);
    
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
    
    const booking = this.selectedBooking();
    this.bookingService.cancelPassenger(passengerId, reason || 'User requested cancel').subscribe({
      next: () => {
        // Optimistically update local state
        this.passengersOfBooking.update(paxList => paxList.map(p => 
          p.id === passengerId ? { ...p, status: 'Cancelled', cancellationReason: reason || 'User requested cancel' } : p
        ));
        // Re-fetch to get true server state (including booking status updates)
        if (booking) {
          this.bookingService.getPassengers(booking.id).subscribe({
            next: (refreshed) => {
              this.passengersOfBooking.set(refreshed);
              // Update booking status from server
              this.bookingService.getUserBookings(this.authService.userId()!).subscribe({
                next: (bks) => this.bookings.set(bks)
              });
            }
          });
        }
        alert('Cancellation successful! Your refund is initiated and will be deposited within 5-6 working days.');
      },
      error: (err: any) => {
        alert(err.error?.message || 'Failed to cancel passenger. Please try again.');
      }
    });
  }

  togglePassengerSelection(id: number) {
    const current = this.selectedPassengerIds();
    if (current.includes(id)) {
        this.selectedPassengerIds.set(current.filter(pId => pId !== id));
    } else {
        this.selectedPassengerIds.set([...current, id]);
    }
  }

  cancelMultipleSelectedPassengers() {
      const selectedIds = this.selectedPassengerIds();
      if (selectedIds.length === 0) return;
      
      const booking = this.selectedBooking();
      if (!booking) return;

      if (!confirm(`Are you sure you want to cancel the selected ${selectedIds.length} passenger(s)?`)) return;
      const reason = prompt('Please enter cancellation reason:');
      if (reason === null) return;
      
      this.bookingService.cancelMultiplePassengers(booking.id, selectedIds, reason || 'User requested cancel').subscribe({
          next: () => {
              this.passengersOfBooking.update(paxList => paxList.map(p => 
                  selectedIds.includes(p.id) ? { ...p, status: 'Cancelled', cancellationReason: reason || 'User requested cancel' } : p
              ));
              this.selectedPassengerIds.set([]); // clear selection
              
              const allCancelled = this.passengersOfBooking().every(p => p.status === 'Cancelled' || p.status === 'Refunded');
              const partial = this.passengersOfBooking().some(p => p.status === 'Cancelled' || p.status === 'Refunded') && !allCancelled;
              
              if(allCancelled) {
                  this.selectedBooking.set( { ...booking, status: 'Cancelled' });
                  this.bookings.update(bs => bs.map(b => b.id === booking.id ? { ...b, status: 'Cancelled' } : b));
              } else if(partial) {
                  this.selectedBooking.set( { ...booking, status: 'PartiallyCancelled' });
                  this.bookings.update(bs => bs.map(b => b.id === booking.id ? { ...b, status: 'PartiallyCancelled' } : b));
              }
          },
          error: (err: any) => {
              alert(err.error?.message || 'Failed to cancel passengers');
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

