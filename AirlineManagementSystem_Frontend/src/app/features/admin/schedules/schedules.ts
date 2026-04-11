import { AirlineTimePipe } from '../../../shared/pipes/airline-time.pipe';
import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { TopNavbarComponent } from '../../../shared/components/top-navbar.component';
import { SideNavbarComponent } from '../../../shared/components/side-navbar.component';
import { FlightService } from '../../../core/services/flight.service';
import { AuthService } from '../../../core/services/auth.service';
import { BookingService } from '../../../core/services/booking.service';
import { TimezoneService } from '../../../core/services/timezone.service';
import { FlightSchedule, Flight, ScheduleBooking } from '../../../core/models/api.models';

@Component({
  selector: 'app-admin-schedules',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TopNavbarComponent, SideNavbarComponent, AirlineTimePipe],
  templateUrl: './schedules.html',
  styleUrl: './schedules.css',
  providers: [DatePipe]
})
export class AdminSchedulesComponent implements OnInit {
  schedules = signal<FlightSchedule[]>([]);
  flights = signal<Flight[]>([]);
  loading = signal(true);
  showForm = signal(false);
  editingSchedule = signal<FlightSchedule | null>(null);
  saving = signal(false);

  // Filters
  filterFlightId = signal<number | null>(null);
  
  // Computed
  filteredSchedules = computed(() => {
    let result = this.schedules();
    if (this.filterFlightId()) {
      result = result.filter(s => s.flightId === this.filterFlightId());
    }
    // Sort by departure time ascending
    return result.sort((a, b) => new Date(a.departureTime).getTime() - new Date(b.departureTime).getTime());
  });

  // Form Mode
  scheduleMode = signal<'single' | 'recurring'>('single');

  // Manifest Mode
  manifestSchedule = signal<FlightSchedule | null>(null);
  manifestBookings = signal<ScheduleBooking[]>([]);
  manifestLoading = signal(false);

  // Single Form fields
  flightId = signal<number>(0);
  departureTime = signal('');
  arrivalTime = signal('');
  
  // Recurring Form fields
  selectedDays = signal<number[]>([]); // 0=Sun, 1=Mon, 2=Tue, 3=Wed, 4=Thu, 5=Fri, 6=Sat
  effectiveFrom = signal('');
  effectiveTo = signal('');
  depTimeOnly = signal('');
  arrTimeOnly = signal('');

  // Shared fields
  gate = signal('');
  status = signal('Scheduled');
  
  economySeats = signal<number | null>(null);
  businessSeats = signal<number | null>(null);
  firstSeats = signal<number | null>(null);
  economyPrice = signal<number | null>(null);
  businessPrice = signal<number | null>(null);
  firstClassPrice = signal<number | null>(null);

  sideNavItems = [
    { icon: 'dashboard', label: 'Dashboard', route: '/admin/dashboard' },
    { icon: 'flight_takeoff', label: 'Fleet Control', route: '/admin/flights' },
    { icon: 'calendar_month', label: 'Scheduling', route: '/admin/schedules' },
    { icon: 'currency_exchange', label: 'Refunds & Cancellations', route: '/admin/refunds' },
    { icon: 'confirmation_number', label: 'Bookings', route: '/admin/dashboard' },
    { icon: 'forklift', label: 'Ground Ops', route: '/admin/dashboard' },
    { icon: 'group', label: 'User Mgmt', route: '/admin/dashboard' },
    { icon: 'settings_applications', label: 'System Config', route: '/admin/dashboard' },
  ];

  constructor(
    public authService: AuthService,
    private flightService: FlightService,
    private bookingService: BookingService,
    private datePipe: DatePipe,
    private timezoneService: TimezoneService
  ) {}

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.loading.set(true);
    // Load both flights (for template lookup) and schedules
    this.flightService.getAllFlights().subscribe({
      next: (f) => {
        this.flights.set(f);
        this.flightService.getAllSchedules().subscribe({
          next: (s) => {
            this.schedules.set(s);
            this.loading.set(false);
          },
          error: () => this.loading.set(false)
        });
      },
      error: () => this.loading.set(false)
    });
  }

  toggleForm() {
    this.showForm.update(v => !v);
    this.editingSchedule.set(null);
    this.scheduleMode.set('single');
    this.resetForm();
  }

  toggleDay(day: number) {
    const current = this.selectedDays();
    if (current.includes(day)) {
      this.selectedDays.set(current.filter(d => d !== day));
    } else {
      this.selectedDays.set([...current, day]);
    }
  }

  editSchedule(schedule: FlightSchedule) {
    this.editingSchedule.set(schedule);
    this.scheduleMode.set('single');
    
    // Format dates for datetime-local
    const toLocalISO = (dateStr: string) => {
        const d = new Date(dateStr);
        // Compute active offset
        const tz = this.timezoneService.getTimezone();
        let offsetMinutes = 0; // UTC
        if (tz === 'IST') offsetMinutes = 330;
        else if (tz === 'EST') offsetMinutes = -300;
        return new Date(d.getTime() + (offsetMinutes * 60000)).toISOString().slice(0, 16);
    };

    this.flightId.set(schedule.flightId);
    this.departureTime.set(toLocalISO(schedule.departureTime));
    this.arrivalTime.set(toLocalISO(schedule.arrivalTime));
    this.gate.set(schedule.gate || '');
    this.status.set(schedule.status);
    
    this.economySeats.set(schedule.economySeats);
    this.businessSeats.set(schedule.businessSeats);
    this.firstSeats.set(schedule.firstSeats);
    this.economyPrice.set(schedule.economyPrice);
    this.businessPrice.set(schedule.businessPrice);
    this.firstClassPrice.set(schedule.firstClassPrice);
    
    this.showForm.set(true);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  resetForm() {
    this.flightId.set(0);
    this.departureTime.set('');
    this.arrivalTime.set('');
    this.gate.set('');
    this.status.set('Scheduled');
    this.selectedDays.set([]);
    this.effectiveFrom.set('');
    this.effectiveTo.set('');
    this.depTimeOnly.set('');
    this.arrTimeOnly.set('');
    
    this.economySeats.set(null);
    this.businessSeats.set(null);
    this.firstSeats.set(null);
    this.economyPrice.set(null);
    this.businessPrice.set(null);
    this.firstClassPrice.set(null);
  }

  async saveSchedule() {
    if (!this.flightId()) {
      alert('Flight Template is required.');
      return;
    }

    this.saving.set(true);
    
    // Shared overrides
    const overrides: any = {
      gate: this.gate()
    };
    if (this.economySeats() !== null) overrides.economySeats = this.economySeats();
    if (this.businessSeats() !== null) overrides.businessSeats = this.businessSeats();
    if (this.firstSeats() !== null) overrides.firstSeats = this.firstSeats();
    if (this.economyPrice() !== null) overrides.economyPrice = this.economyPrice();
    if (this.businessPrice() !== null) overrides.businessPrice = this.businessPrice();
    if (this.firstClassPrice() !== null) overrides.firstClassPrice = this.firstClassPrice();

    if (this.scheduleMode() === 'single') {
        if (!this.departureTime() || !this.arrivalTime()) {
            alert('Departure Time and Arrival Time are required.');
            this.saving.set(false);
            return;
        }

        const tz = this.timezoneService.getTimezone();
        let os = '+00:00';
        if (tz === 'IST') os = '+05:30';
        else if (tz === 'EST') os = '-05:00';

        const data: any = {
          flightId: this.flightId(),
          departureTime: new Date(this.departureTime() + os).toISOString(),
          arrivalTime: new Date(this.arrivalTime() + os).toISOString(),
          status: this.status(),
          ...overrides
        };

        const currEdit = this.editingSchedule();
        if (currEdit) {
            this.flightService.updateSchedule(currEdit.id, data).subscribe({
                next: () => { this.finalizeSave(); },
                error: (err) => { this.saving.set(false); alert('Error updating schedule'); console.error(err); }
            });
        } else {
            this.flightService.createSchedule(data).subscribe({
                next: () => { this.finalizeSave(); },
                error: (err) => { this.saving.set(false); alert('Error creating schedule'); console.error(err); }
            });
        }
    } else {
        // Recurring processing
        if (!this.effectiveFrom() || !this.effectiveTo() || !this.depTimeOnly() || !this.arrTimeOnly() || this.selectedDays().length === 0) {
            alert('Please fill out all recurring fields and select at least one day.');
            this.saving.set(false);
            return;
        }

        const start = new Date(this.effectiveFrom());
        const end = new Date(this.effectiveTo());
        
        const depTimeParts = this.depTimeOnly().split(':').map(Number);
        const arrTimeParts = this.arrTimeOnly().split(':').map(Number);

        // Calculate arrival day offset (if arrival is earlier than departure, it's next day)
        const depMs = depTimeParts[0] * 60 + depTimeParts[1];
        const arrMs = arrTimeParts[0] * 60 + arrTimeParts[1];
        const nextDayArrival = arrMs <= depMs;

        const payloads: any[] = [];
        
        const tz = this.timezoneService.getTimezone();
        let os = '+00:00';
        if (tz === 'IST') os = '+05:30';
        else if (tz === 'EST') os = '-05:00';
        
        const iterDate = new Date(`${start.toISOString().slice(0, 10)}T00:00:00${os}`);
        const iterEndDate = new Date(`${end.toISOString().slice(0, 10)}T23:59:59${os}`);
        
        while (iterDate <= iterEndDate) {
            if (this.selectedDays().includes(iterDate.getDay())) {
                const dateStr = iterDate.toISOString().slice(0, 10);
                
                // Construct requested explicit string and let angular convert it to UTC via ToISOString()
                // iterDate at this point represents the correct date logic in local context
                
                const dep = new Date(`${dateStr}T${this.depTimeOnly().padStart(5, '0')}:00${os}`);
                
                const arr = new Date(`${dateStr}T${this.arrTimeOnly().padStart(5, '0')}:00${os}`);
                if (nextDayArrival) arr.setDate(arr.getDate() + 1);

                payloads.push({
                    flightId: this.flightId(),
                    departureTime: dep.toISOString(),
                    arrivalTime: arr.toISOString(),
                    status: 'Scheduled',
                    ...overrides
                });
            }
            iterDate.setDate(iterDate.getDate() + 1);
        }

        if (payloads.length === 0) {
            alert('No dates matched the given pattern.');
            this.saving.set(false);
            return;
        }

        if (payloads.length > 365) {
            alert('Cannot create more than 365 schedules at once. Please shorten the date range.');
            this.saving.set(false);
            return;
        }

        // Sequential Creation to avoid bursting backend
        try {
            for (const payload of payloads) {
                await new Promise<void>((resolve, reject) => {
                    this.flightService.createSchedule(payload).subscribe({
                        next: () => resolve(),
                        error: (err) => reject(err)
                    });
                });
            }
            this.finalizeSave();
        } catch (e) {
            alert('Error during bulk creation. Some schedules may not have been created.');
            console.error(e);
            this.saving.set(false);
        }
    }
  }

  finalizeSave() {
    this.saving.set(false);
    this.showForm.set(false);
    this.loadData();
  }

  cancelSchedule(id: number) {
    if (confirm('Are you sure you want to cancel this schedule?')) {
        this.flightService.cancelSchedule(id).subscribe({
            next: () => this.loadData(),
            error: (err) => console.error(err)
        });
    }
  }

  deleteSchedule(id: number) {
    if (confirm('WARNING: Deleting a schedule is permanent. Are you sure?')) {
        this.flightService.deleteSchedule(id).subscribe({
            next: () => this.loadData(),
            error: (err) => console.error(err)
        });
    }
  }

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'scheduled': return 'st-blue';
      case 'delayed': return 'st-amber';
      case 'cancelled': return 'st-red';
      case 'completed': return 'st-green';
      default: return 'st-default';
    }
  }

  viewManifest(schedule: FlightSchedule) {
    this.manifestSchedule.set(schedule);
    this.manifestLoading.set(true);
    this.bookingService.getBookingsBySchedule(schedule.id).subscribe({
      next: (bookings) => {
        this.manifestBookings.set(bookings);
        this.manifestLoading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.manifestLoading.set(false);
      }
    });
  }

  closeManifest() {
    this.manifestSchedule.set(null);
    this.manifestBookings.set([]);
  }
}

