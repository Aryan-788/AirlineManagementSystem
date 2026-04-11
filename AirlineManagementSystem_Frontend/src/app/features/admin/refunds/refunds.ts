import { AirlineTimePipe } from '../../../shared/pipes/airline-time.pipe';
import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TopNavbarComponent } from '../../../shared/components/top-navbar.component';
import { SideNavbarComponent } from '../../../shared/components/side-navbar.component';
import { BookingService } from '../../../core/services/booking.service';
import { RefundDetails } from '../../../core/models/api.models';

@Component({
  selector: 'app-admin-refunds',
  standalone: true,
  imports: [CommonModule, RouterModule, TopNavbarComponent, SideNavbarComponent, AirlineTimePipe],
  templateUrl: './refunds.html',
  styleUrl: './refunds.css'
})
export class AdminRefundsComponent implements OnInit {
  refunds = signal<RefundDetails[]>([]);
  loading = signal(true);
  
  sideNavItems = [
    { icon: 'dashboard', label: 'Dashboard', route: '/admin/dashboard' },
    { icon: 'flight_takeoff', label: 'Fleet Control', route: '/admin/flights' },
    { icon: 'calendar_month', label: 'Scheduling', route: '/admin/schedules' },
    { icon: 'currency_exchange', label: 'Refunds & Cancellations', route: '/admin/refunds' },
  ];

  constructor(private bookingService: BookingService) {}

  ngOnInit() {
    this.bookingService.getAllRefunds().subscribe({
      next: (data) => {
        this.refunds.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error fetching refunds', err);
        this.loading.set(false);
      }
    });
  }

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'processed': 
      case 'refunded': return 'st-green';
      case 'pending':
      case 'refundpending': return 'st-amber';
      case 'failed': return 'st-red';
      default: return 'st-default';
    }
  }
}
