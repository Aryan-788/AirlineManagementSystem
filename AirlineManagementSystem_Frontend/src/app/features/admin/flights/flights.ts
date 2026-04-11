import { AirlineTimePipe } from '../../../shared/pipes/airline-time.pipe';
import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { TopNavbarComponent } from '../../../shared/components/top-navbar.component';
import { SideNavbarComponent } from '../../../shared/components/side-navbar.component';
import { FlightService } from '../../../core/services/flight.service';
import { AuthService } from '../../../core/services/auth.service';
import { Flight } from '../../../core/models/api.models';

@Component({
  selector: 'app-admin-flights',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TopNavbarComponent, SideNavbarComponent, AirlineTimePipe],
  templateUrl: './flights.html',
  styleUrl: './flights.css'
})
export class AdminFlightsComponent implements OnInit {
  flights = signal<Flight[]>([]);
  loading = signal(true);
  showForm = signal(false);
  editingFlight = signal<Flight | null>(null);
  saving = signal(false);

  // Form fields
  flightNumber = signal('');
  source = signal('');
  destination = signal('');
  departureTime = signal('');
  arrivalTime = signal('');
  aircraft = signal('');
  totalSeats = signal(180);
  economySeats = signal(120);
  businessSeats = signal(40);
  firstSeats = signal(20);
  economyPrice = signal(3500);
  businessPrice = signal(9000);
  firstClassPrice = signal(18000);

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

  activityLog = [
    { icon: 'check_circle', msg: 'GA-104 gate assigned to B12', time: '2m ago', color: '#16a34a' },
    { icon: 'warning', msg: 'GA-922 delayed by 45 min', time: '15m ago', color: '#d97706' },
    { icon: 'cancel', msg: 'GA-411 cancelled', time: '1h ago', color: '#dc2626' },
    { icon: 'flight_takeoff', msg: 'GA-001 departed on time', time: '2h ago', color: '#2563eb' },
  ];

  constructor(
    public authService: AuthService,
    private flightService: FlightService
  ) {}

  ngOnInit() { this.loadFlights(); }

  loadFlights() {
    this.loading.set(true);
    this.flightService.getAllFlights().subscribe({
      next: (data) => { this.flights.set(data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  toggleForm() { this.showForm.update(v => !v); this.editingFlight.set(null); this.resetForm(); }

  editFlight(flight: Flight) {
    this.editingFlight.set(flight);
    this.flightNumber.set(flight.flightNumber);
    this.source.set(flight.source);
    this.destination.set(flight.destination);
    
    // Format dates for datetime-local (force IST display)
    const toLocalISO = (dateStr: string) => {
        const d = new Date(dateStr);
        // Convert to offset +05:30 (330 mins)
        return new Date(d.getTime() + (330 * 60000)).toISOString().slice(0, 16);
    };
    
    this.departureTime.set(toLocalISO(flight.departureTime));
    this.arrivalTime.set(toLocalISO(flight.arrivalTime));
    this.aircraft.set(flight.aircraft);
    this.totalSeats.set(flight.totalSeats);
    this.economySeats.set(flight.economySeats);
    this.businessSeats.set(flight.businessSeats);
    this.firstSeats.set(flight.firstSeats);
    this.economyPrice.set(flight.economyPrice);
    this.businessPrice.set(flight.businessPrice);
    this.firstClassPrice.set(flight.firstClassPrice);
    
    this.showForm.set(true);
  }

  resetForm() {
    this.flightNumber.set(''); this.source.set(''); this.destination.set('');
    this.departureTime.set(''); this.arrivalTime.set(''); this.aircraft.set('');
    this.totalSeats.set(180); this.economySeats.set(120); this.businessSeats.set(40); this.firstSeats.set(20);
    this.economyPrice.set(3500); this.businessPrice.set(9000); this.firstClassPrice.set(18000);
  }

  saveFlight() {
    this.saving.set(true);
    const data = {
      flightNumber: this.flightNumber(), source: this.source(), destination: this.destination(),
      departureTime: new Date(this.departureTime() + '+05:30').toISOString(), arrivalTime: new Date(this.arrivalTime() + '+05:30').toISOString(), aircraft: this.aircraft(),
      totalSeats: this.totalSeats(), economySeats: this.economySeats(), businessSeats: this.businessSeats(), firstSeats: this.firstSeats(),
      economyPrice: this.economyPrice(), businessPrice: this.businessPrice(), firstClassPrice: this.firstClassPrice()
    };
    
    const currEdit = this.editingFlight();
    if (currEdit) {
      this.flightService.updateFlight(currEdit.id, data).subscribe({
        next: () => { this.saving.set(false); this.showForm.set(false); this.editingFlight.set(null); this.loadFlights(); },
        error: () => this.saving.set(false)
      });
    } else {
      this.flightService.createFlight(data as any).subscribe({
        next: () => { this.saving.set(false); this.showForm.set(false); this.loadFlights(); },
        error: () => this.saving.set(false)
      });
    }
  }

  deleteFlight(id: number) {
    this.flightService.deleteFlight(id).subscribe({ next: () => this.loadFlights() });
  }

  cancelFlight(id: number) {
    this.flightService.cancelFlight(id).subscribe({ next: () => this.loadFlights() });
  }

  assignGate(id: number) {
    const gate = prompt('Enter gate number (e.g. B12):');
    if (gate) this.flightService.assignGate(id, gate).subscribe({ next: () => this.loadFlights() });
  }

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'scheduled': return 'st-blue';
      case 'delayed': return 'st-amber';
      case 'cancelled': return 'st-red';
      case 'departed': case 'landed': return 'st-green';
      default: return 'st-default';
    }
  }
}

