import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { TopNavbarComponent } from '../../../shared/components/top-navbar.component';
import { FlightService } from '../../../core/services/flight.service';
import { AuthService } from '../../../core/services/auth.service';
import { FlightSchedule } from '../../../core/models/api.models';

@Component({
  selector: 'app-flight-search',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TopNavbarComponent],
  templateUrl: './search.html',
  styleUrl: './search.css'
})
export class FlightSearchComponent {
  source = signal('');
  destination = signal('');
  departureDate = signal('');
  Math = Math;

  allSchedules = signal<FlightSchedule[]>([]);
  loading = signal(false);
  hasSearched = signal(false);

  passengers = signal(1);
  travelClass = signal('Economy');
  sortOrder = signal('price_asc');

  cities = ['Mumbai', 'Delhi', 'Bengaluru', 'Hyderabad', 'Chennai', 'Kolkata', 'Goa', 'Patna', 'Amritsar', 'Indore', 'Ahmedabad', 'Pune'];
  showSourceDropdown = signal(false);
  showDestDropdown = signal(false);
  
  filteredSources = computed(() => this.cities.filter(c => c.toLowerCase().includes(this.source().toLowerCase())));
  filteredDests = computed(() => this.cities.filter(c => c.toLowerCase().includes(this.destination().toLowerCase())));

  selectSource(c: string) { this.source.set(c); this.showSourceDropdown.set(false); }
  selectDest(c: string) { this.destination.set(c); this.showDestDropdown.set(false); }
  
  onSourceBlur() { setTimeout(() => this.showSourceDropdown.set(false), 200); }
  onDestBlur() { setTimeout(() => this.showDestDropdown.set(false), 200); }

  // Filters
  filterMaxPrice = signal(100000);
  filterDepartureTime = signal<'all' | 'morning' | 'afternoon' | 'evening' | 'night'>('all');
  filterStatusScheduled = signal(true);
  filterStatusDelayed = signal(true);
  showFilterPanel = signal(false);

  constructor(
    private flightService: FlightService,
    public authService: AuthService,
    private router: Router
  ) {}

  get flights(): FlightSchedule[] {
    let results = [...this.allSchedules()];

    // Price filter
    results = results.filter(f => this.getCalculatedPrice(f) <= this.filterMaxPrice());

    // Departure time filter
    const timeFilter = this.filterDepartureTime();
    if (timeFilter !== 'all') {
      results = results.filter(f => {
        const hourStr = new Date(f.departureTime).toLocaleString('en-US', { timeZone: 'Asia/Kolkata', hour: 'numeric', hour12: false });
        const hour = parseInt(hourStr, 10);
        if (timeFilter === 'morning') return hour >= 5 && hour < 12;
        if (timeFilter === 'afternoon') return hour >= 12 && hour < 17;
        if (timeFilter === 'evening') return hour >= 17 && hour < 21;
        if (timeFilter === 'night') return hour >= 21 || hour < 5;
        return true;
      });
    }

    // Status filter
    results = results.filter(f => {
      const s = f.status?.toLowerCase();
      if (s === 'scheduled') return this.filterStatusScheduled();
      if (s === 'delayed') return this.filterStatusDelayed();
      // Completed / Cancelled are excluded from search view typically
      if (s === 'cancelled' || s === 'completed') return false; 
      return true;
    });

    // Seat availability filter based on requested passenger count
    results = results.filter(s => {
      const requested = this.passengers();
      if (this.travelClass() === 'Economy' && s.economySeats < requested) return false;
      if (this.travelClass() === 'Business' && s.businessSeats < requested) return false;
      if (this.travelClass() === 'First Class' && s.firstSeats < requested) return false;
      return true;
    });

    // Sort Results
    if (this.sortOrder() === 'price_asc') {
      results.sort((a, b) => this.getCalculatedPrice(a) - this.getCalculatedPrice(b));
    } else if (this.sortOrder() === 'price_desc') {
      results.sort((a, b) => this.getCalculatedPrice(b) - this.getCalculatedPrice(a));
    }

    return results;
  }

  get minPrice(): number {
    return 3000;
  }

  get maxPrice(): number {
    const prices = this.allSchedules().map(f => this.getCalculatedPrice(f));
    const maxCalculated = prices.length ? Math.max(...prices) : 100000;
    return Math.max(maxCalculated, 100000);
  }

  getCalculatedPrice(flight: FlightSchedule): number {
    let basePrice = flight.economyPrice || 0;
    
    switch (this.travelClass()) {
      case 'Business':
        basePrice = flight.businessPrice || basePrice * 2.5;
        break;
      case 'First Class':
        basePrice = flight.firstClassPrice || basePrice * 4.0;
        break;
    }
    
    return basePrice * this.passengers();
  }

  searchFlights() {
    if (!this.source() || !this.destination() || !this.departureDate()) return;
    this.loading.set(true);
    this.hasSearched.set(true);

    this.flightService.searchSchedules(this.source(), this.destination(), this.departureDate()).subscribe({
      next: (data) => { 
        this.allSchedules.set(data); 
        this.resetFilters(); 
        this.loading.set(false); 
      },
      error: () => { 
        this.allSchedules.set([]); 
        this.loading.set(false); 
      }
    });
  }

  loadAllFlights() {
    this.loading.set(true);
    this.hasSearched.set(true);
    this.flightService.getAllSchedules().subscribe({
      next: (data) => {
        this.allSchedules.set(data);
        this.filterMaxPrice.set(this.maxPrice);
        this.loading.set(false);
      },
      error: () => { this.allSchedules.set([]); this.loading.set(false); }
    });
  }

  bookFlight(scheduleId: number) {
    const rolePrefix = this.authService.userRole() === 'Dealer' ? 'dealer' : 'passenger';
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: `/${rolePrefix}/booking/schedule/${scheduleId}` }});
      return;
    }

    const queryParams = {
      class: this.travelClass(),
      passengers: this.passengers()
    };
    
    // Redirect to schedule booking route
    this.router.navigate([`/${rolePrefix}/booking/schedule`, scheduleId], { queryParams });
  }

  resetFilters() {
    this.filterMaxPrice.set(this.maxPrice);
    this.filterDepartureTime.set('all');
    this.filterStatusScheduled.set(true);
    this.filterStatusDelayed.set(true);
  }

  formatTime(dateStr: string): string {
    if (!dateStr) return '--:--';
    const d = new Date(dateStr);
    return d.toLocaleTimeString('en-IN', { timeZone: 'Asia/Kolkata', hour: '2-digit', minute: '2-digit', hour12: false });
  }

  getDuration(dep: string, arr: string): string {
    if (!dep || !arr) return '--';
    const diff = new Date(arr).getTime() - new Date(dep).getTime();
    const h = Math.floor(diff / 3600000);
    const m = Math.floor((diff % 3600000) / 60000);
    return `${h}h ${m}m`;
  }

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'scheduled': return 'tag-scheduled';
      case 'delayed': return 'tag-delayed';
      case 'cancelled': return 'tag-cancelled';
      case 'completed': return 'tag-completed';
      default: return 'tag-default';
    }
  }
}
