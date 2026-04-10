import { AirlineTimePipe } from '../../../shared/pipes/airline-time.pipe';
import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TopNavbarComponent } from '../../../shared/components/top-navbar.component';
import { SideNavbarComponent } from '../../../shared/components/side-navbar.component';
import { AdminService } from '../../../core/services/admin.service';
import { FlightService } from '../../../core/services/flight.service';
import { BookingService } from '../../../core/services/booking.service';
import { AuthService } from '../../../core/services/auth.service';
import { AdminDashboard, Flight, Booking } from '../../../core/models/api.models';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, TopNavbarComponent, SideNavbarComponent, AirlineTimePipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class AdminDashboardComponent implements OnInit {
  stats = signal<AdminDashboard>({ totalBookings: 0, totalRevenue: 0, activeFlights: 0, totalUsers: 0 });
  flights = signal<Flight[]>([]);
  recentBookings = signal<Booking[]>([]);
  loading = signal(true);
  today = signal(new Date().toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' }));
  
  // Dynamic Ticket Distribution
  ticketDist = signal({
    economyPct: 65, businessPct: 20, firstPct: 15, totalBooked: 100
  });

  sideNavItems = [
    { icon: 'dashboard', label: 'Dashboard', route: '/admin/dashboard' },
    { icon: 'flight_takeoff', label: 'Fleet Control', route: '/admin/flights' },
    { icon: 'calendar_month', label: 'Scheduling', route: '/admin/schedules' },
    { icon: 'confirmation_number', label: 'Bookings', route: '/admin/dashboard' },
    { icon: 'forklift', label: 'Ground Ops', route: '/admin/dashboard' },
    { icon: 'group', label: 'User Mgmt', route: '/admin/dashboard' },
    { icon: 'settings_applications', label: 'System Config', route: '/admin/dashboard' },
  ];

  constructor(
    public authService: AuthService,
    private adminService: AdminService,
    private flightService: FlightService
  ) {}

  ngOnInit() {
    this.adminService.getDashboard().subscribe({
      next: (d) => { 
        this.stats.set({
          totalBookings: d.totalBookings,
          totalRevenue: d.totalRevenue,
          activeFlights: d.activeFlights,
          totalUsers: d.totalUsers
        });
      },
      error: () => {}
    });
    this.flightService.getAllFlights().subscribe({
      next: (f) => {
        this.flights.set(f);
        let bookings = 0;
        let totalEcon = 0, totalBus = 0, totalFirst = 0;

        f.forEach(flight => {
          // Keep for pie chart structure simulation if needed
          const capacity = flight.totalSeats > 0 ? flight.totalSeats : 180;
          const booked = Math.max(0, capacity - flight.availableSeats);
          
          if (booked > 0) {
            bookings += booked;
            totalEcon += Math.floor(booked * 0.7);
            totalBus += Math.floor(booked * 0.2);
            totalFirst += Math.floor(booked * 0.1);
          }
        });

        const totalDistrib = totalEcon + totalBus + totalFirst || 1;
        this.ticketDist.set({
          economyPct: Math.round((totalEcon / totalDistrib) * 100),
          businessPct: Math.round((totalBus / totalDistrib) * 100),
          firstPct: Math.round((totalFirst / totalDistrib) * 100),
          totalBooked: bookings
        });
        
        
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'confirmed': return 'st-green';
      case 'pending': return 'st-amber';
      case 'cancelled': return 'st-red';
      case 'scheduled': return 'st-blue';
      default: return 'st-default';
    }
  }
}

