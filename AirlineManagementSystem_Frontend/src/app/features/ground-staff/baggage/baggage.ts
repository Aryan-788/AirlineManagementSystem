import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { TopNavbarComponent } from '../../../shared/components/top-navbar.component';
import { SideNavbarComponent } from '../../../shared/components/side-navbar.component';
import { BaggageService } from '../../../core/services/baggage.service';
import { CheckInService } from '../../../core/services/checkin.service';
import { BookingService } from '../../../core/services/booking.service';
import { FlightService } from '../../../core/services/flight.service';
import { AuthService } from '../../../core/services/auth.service';
import { Baggage, BaggageSummary, CheckInSummary, Booking, Flight, Passenger } from '../../../core/models/api.models';

@Component({
  selector: 'app-ground-staff-baggage',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TopNavbarComponent, SideNavbarComponent],
  templateUrl: './baggage.html',
  styleUrl: './baggage.css'
})
export class GroundStaffBaggageComponent implements OnInit {
  baggages = signal<Baggage[]>([]);
  loading = signal(true);
  trackingNumber = signal('');
  baggageSummary = signal<BaggageSummary | null>(null);
  checkInSummary = signal<CheckInSummary | null>(null);

  // New Bag Form
  showAddModal = signal(false);
  bookingSearchTerm = signal(''); // User enters ID or PNR here
  newBookingId = signal<number | null>(null); // Numeric ID resolved from search
  newWeight = signal<number | null>(null);
  newPassengerName = signal('');
  newFlightNumber = signal('');
  formError = signal('');
  formSuccess = signal('');
  isAutoFilling = signal(false);

  // Status mapping for the UI buttons
  statusOptions = ['Checked', 'InTransit', 'Loaded', 'Delivered'];

  sideNavItems = [
    { icon: 'dashboard', label: 'Dashboard', route: '/ground-staff/baggage' },
    { icon: 'flight_takeoff', label: 'Fleet Control', route: '/ground-staff/baggage' },
    { icon: 'confirmation_number', label: 'Bookings', route: '/ground-staff/baggage' },
    { icon: 'forklift', label: 'Ground Ops', route: '/ground-staff/baggage' },
    { icon: 'group', label: 'User Mgmt', route: '/ground-staff/baggage' },
    { icon: 'settings_applications', label: 'System Config', route: '/ground-staff/baggage' },
  ];

  eventLog = [
    { color: '#16a34a', event: 'BAGGAGE_SCAN_SUCCESS', detail: 'Gate A12 - Handler #4299', time: '12s ago' },
    { color: '#2563eb', event: 'FLIGHT_MANIFEST_UPDATED', detail: 'GA-442 Full Payload Sync', time: '2m ago' },
    { color: '#d97706', event: 'DELAY_ADVISORY_RECEIVED', detail: 'GA-109 Loading Lag Detected', time: '5m ago' },
    { color: '#94a3b8', event: 'HEARTBEAT_ACK', detail: 'RabbitMQ Cluster Node 01', time: '8m ago' },
  ];

  constructor(
    public authService: AuthService,
    private baggageService: BaggageService,
    private checkInService: CheckInService,
    private bookingService: BookingService,
    private flightService: FlightService
  ) {}

  ngOnInit() {
    this.refreshSummary();
    this.checkInService.getSummary().subscribe({
      next: (s) => this.checkInSummary.set(s)
    });
    this.loadAllBaggage();
  }

  loadAllBaggage() {
    this.loading.set(true);
    this.baggageService.getAllBaggage().subscribe({
      next: (data) => {
        this.baggages.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  trackBaggage() {
    if (!this.trackingNumber()) return;
    this.loading.set(true);
    
    // Check if it's a numeric Booking ID or a string PNR/Tracking ID
    const input = this.trackingNumber().trim();
    if (/^\d+$/.test(input)) {
       this.baggageService.getBaggageByBooking(parseInt(input)).subscribe({
        next: (bs) => {
          this.baggages.set(bs);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
       });
    } else {
      this.baggageService.trackBaggage(input).subscribe({
        next: (b) => {
          this.baggages.set([b]);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
    }
  }

  openAddModal() {
    this.showAddModal.set(true);
    this.formError.set('');
    this.formSuccess.set('');
  }

  closeAddModal() {
    this.showAddModal.set(false);
    this.bookingSearchTerm.set('');
    this.newBookingId.set(null);
    this.newWeight.set(null);
    this.newPassengerName.set('');
    this.newFlightNumber.set('');
    this.formError.set('');
    this.formSuccess.set('');
  }

  fetchBookingDetails() {
    const term = this.bookingSearchTerm().trim();
    if (!term) {
      this.formError.set('Please enter a PNR or Booking ID.');
      return;
    }

    this.isAutoFilling.set(true);
    this.formError.set('');
    this.newPassengerName.set('');
    this.newFlightNumber.set('');

    const isNumeric = /^\d+$/.test(term);

    const onDetailsFetched = (booking: Booking) => {
      this.newBookingId.set(booking.id);
      
      // 2. Get Flight Details for Flight Number
      this.flightService.getFlightById(booking.flightId).subscribe({
        next: (flight: Flight) => this.newFlightNumber.set(flight.flightNumber),
        error: () => this.formError.set('Could not fetch flight details.')
      });

      // 3. Get Passengers for Passenger Name (take the first one)
      this.bookingService.getPassengers(booking.id).subscribe({
        next: (pax: Passenger[]) => {
          if (pax.length > 0) this.newPassengerName.set(pax[0].name);
          else this.formError.set('No passengers found for this booking.');
        },
        error: () => this.formError.set('Could not fetch passenger details.')
      });
    };

    const onError = (err: any) => {
      this.formError.set(err.error?.message || 'Booking/PNR not found.');
      this.isAutoFilling.set(false);
    };

    if (isNumeric) {
      // Try ID first
      this.bookingService.getBooking(parseInt(term)).subscribe({
        next: onDetailsFetched,
        error: () => {
          // If ID fails, try as PNR (some PNRs might be numeric)
          this.bookingService.getBookingByPnr(term).subscribe({
            next: onDetailsFetched,
            error: onError,
            complete: () => this.isAutoFilling.set(false)
          });
        },
        complete: () => this.isAutoFilling.set(false)
      });
    } else {
      // Must be PNR
      this.bookingService.getBookingByPnr(term).subscribe({
        next: onDetailsFetched,
        error: onError,
        complete: () => this.isAutoFilling.set(false)
      });
    }
  }

  addBaggage() {
    if (!this.newBookingId() || !this.newWeight() || !this.newPassengerName() || !this.newFlightNumber()) {
      this.formError.set('Please fill all fields (Booking ID, Weight, Name, and Flight).');
      return;
    }

    if (this.newWeight()! > 23) {
      this.formError.set('Baggage weight exceeds the maximum allowed limit of 23kg.');
      return;
    }

    this.loading.set(true);
    this.baggageService.addBaggage(
      this.newBookingId()!, 
      this.newWeight()!, 
      this.newPassengerName(), 
      this.newFlightNumber()
    ).subscribe({
      next: (bag) => {
        this.baggages.update(prev => [bag, ...prev]);
        this.formSuccess.set('Baggage added successfully!');
        this.refreshSummary();
        setTimeout(() => this.closeAddModal(), 1500);
      },
      error: (err) => {
        this.formError.set(err.error?.message || 'Failed to add baggage.');
      },
      complete: () => this.loading.set(false)
    });
  }

  refreshSummary() {
    this.baggageService.getSummary().subscribe(s => this.baggageSummary.set(s));
  }

  updateStatus(id: number, status: string) {
    this.baggageService.updateBaggageStatus(id, status).subscribe({
      next: () => {
        this.baggages.update(bags => bags.map(b => b.id === id ? { ...b, status, isDelivered: status === 'Delivered' } : b));
        this.refreshSummary();
      },
      error: () => {}
    });
  }

  markDelivered(id: number) {
    this.baggageService.markDelivered(id).subscribe({
      next: () => {
        this.baggages.update(bags => bags.map(b => b.id === id ? { ...b, isDelivered: true, status: 'Delivered' } : b));
        this.refreshSummary();
      },
      error: () => {}
    });
  }

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'loading': return 'st-loading';
      case 'loaded': return 'st-loaded';
      case 'delivered': return 'st-delivered';
      case 'at carousel': return 'st-carousel';
      case 'checked-in': return 'st-checkedin';
      default: return 'st-default';
    }
  }
}
