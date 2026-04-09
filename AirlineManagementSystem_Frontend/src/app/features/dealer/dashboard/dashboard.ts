import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { TopNavbarComponent } from '../../../shared/components/top-navbar.component';
import { SideNavbarComponent } from '../../../shared/components/side-navbar.component';
import { AgentService } from '../../../core/services/agent.service';
import { FlightService } from '../../../core/services/flight.service';
import { BookingService } from '../../../core/services/booking.service';
import { AuthService } from '../../../core/services/auth.service';
import { CommissionReport, DealerPerformance, Flight, Booking } from '../../../core/models/api.models';

@Component({
  selector: 'app-dealer-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TopNavbarComponent, SideNavbarComponent],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class DealerDashboardComponent implements OnInit {
  commissions = signal<CommissionReport[]>([]);
  flights = signal<Flight[]>([]);
  recentBookings = signal<Booking[]>([]);
  performance = signal<DealerPerformance | null>(null);
  loading = signal(true);
  source = signal('');
  destination = signal('');
  departureDate = signal('');
  seatClass = signal('Economy');
  today = new Date().toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });

  sideNavItems = [
    { icon: 'dashboard', label: 'Dashboard', route: '/dealer/dashboard' },
    { icon: 'travel_explore', label: 'Search Flights', route: '/dealer/search' },
    { icon: 'confirmation_number', label: 'My Bookings', route: '/dealer/my-bookings' },
  ];

  constructor(
    public authService: AuthService,
    private agentService: AgentService,
    private flightService: FlightService,
    private bookingService: BookingService
  ) {}

  ngOnInit() {
    const email = this.authService.userEmail();
    if (email) {
      this.agentService.getDealerByEmail(email).subscribe({
        next: (dealer) => {
          this.agentService.getDealerPerformance(dealer.id).subscribe({
            next: (p) => { this.performance.set(p); },
            error: () => {}
          });

          // Fetch recent bookings using the correct user ID
          this.bookingService.getBookingHistory(this.authService.userId()).subscribe({
            next: (b) => this.recentBookings.set(b.slice(0, 5)),
            error: () => {}
          });
        },
        error: () => {
          console.error('[Dashboard] Dealer not found by email:', email);
        }
      });
    }

    this.agentService.getCommissionReport().subscribe({
      next: (d) => { this.commissions.set(d); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
    this.flightService.getAllFlights().subscribe({
      next: (f) => this.flights.set(f.slice(0, 5)),
      error: () => {}
    });

  }

  searchAllocations() {
    if (!this.source() || !this.destination() || !this.departureDate()) return;
    this.flightService.searchFlights(this.source(), this.destination(), this.departureDate()).subscribe({
      next: (f) => this.flights.set(f),
      error: () => {}
    });
  }
}
