import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { TopNavbarComponent } from '../../../shared/components/top-navbar.component';
import { FlightService } from '../../../core/services/flight.service';
import { BookingService } from '../../../core/services/booking.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { Flight, FlightSchedule, CreatePassengerRequest } from '../../../core/models/api.models';

@Component({
  selector: 'app-booking',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TopNavbarComponent],
  templateUrl: './booking.html',
  styleUrl: './booking.css'
})
export class BookingComponent implements OnInit {
  flight = signal<Flight | null>(null);
  scheduleId = signal<number | null>(null);
  seatClass = signal('Economy');
  
  passengers = signal<CreatePassengerRequest[]>([
    { name: '', age: 25, gender: 'Male', aadharCardNo: '' }
  ]);

  baggageWeight = signal(0);
  loading = signal(false);
  step = signal(1);
  selectedSeats = signal<string[]>([]);
  occupiedSeats = signal<string[]>([]);

  economySeats: string[] = [];
  businessSeats: string[] = [];
  firstSeats: string[] = [];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private flightService: FlightService,
    private bookingService: BookingService,
    public authService: AuthService,
    private toastService: ToastService
  ) {}

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
        if(params['class']) this.seatClass.set(params['class']);
        const reqPax = Number(params['passengers']);
        if(reqPax > 1) {
            this.passengers.set(Array(reqPax).fill(0).map(() => ({ name: '', age: 25, gender: 'Male', aadharCardNo: '' })));
        }
    });

    const sId = this.route.snapshot.paramMap.get('scheduleId');
    const fId = this.route.snapshot.paramMap.get('id');

    if (sId) {
        this.scheduleId.set(Number(sId));
        this.flightService.getScheduleById(Number(sId)).subscribe({
            next: (s: FlightSchedule) => {
                // Map schedule to flight interface for shared template usage
                const mockFlight: Flight = {
                    ...s,
                    id: s.flightId // Important: base flight ID goes here
                } as Flight;
                this.flight.set(mockFlight);
                this.generateSeats(mockFlight);
                this.bookingService.getOccupiedSeats(mockFlight.id, Number(sId)).subscribe({
                  next: (seats) => this.occupiedSeats.set(seats)
                });
            },
            error: () => this.router.navigate([`/${this.authService.userRole() === 'Dealer' ? 'dealer' : 'passenger'}/search`])
        });
    } else if (fId) {
        this.flightService.getFlightById(Number(fId)).subscribe({
          next: (f) => {
            this.flight.set(f);
            this.generateSeats(f);
            this.bookingService.getOccupiedSeats(f.id).subscribe({
              next: (seats) => this.occupiedSeats.set(seats)
            });
          },
          error: () => this.router.navigate([`/${this.authService.userRole() === 'Dealer' ? 'dealer' : 'passenger'}/search`])
        });
    } else {
        this.router.navigate([`/${this.authService.userRole() === 'Dealer' ? 'dealer' : 'passenger'}/search`]);
    }
    // Do not auto-fill passenger name by default as per user request
  }

  generateSeats(flight: Flight) {
    const rows = ['A', 'B', 'C', 'D'];
    this.firstSeats = [];
    for (let i = 1; i <= Math.min(Math.ceil(flight.firstSeats / 4), 3); i++) {
      rows.forEach(r => this.firstSeats.push(`${i}${r}`));
    }
    this.businessSeats = [];
    for (let i = 4; i <= Math.min(3 + Math.ceil(flight.businessSeats / 4), 8); i++) {
      rows.forEach(r => this.businessSeats.push(`${i}${r}`));
    }
    this.economySeats = [];
    for (let i = 10; i <= Math.min(9 + Math.ceil(flight.economySeats / 6), 25); i++) {
      ['A', 'B', 'C', 'D', 'E', 'F'].forEach(r => {
         if (this.economySeats.length < flight.economySeats) this.economySeats.push(`${i}${r}`);
      });
    }
  }

  selectSeat(seat: string, cls: string) {
    if (this.occupiedSeats().includes(seat)) return;

    if (this.selectedSeats().includes(seat)) {
      this.selectedSeats.update(s => s.filter(x => x !== seat));
      if (this.selectedSeats().length === 0) {
        this.seatClass.set('Economy');
      }
      return;
    }

    if (this.selectedSeats().length > 0 && this.seatClass() !== cls) {
      if (!confirm('Selecting a different class will clear your current seat selections. Continue?')) {
        return;
      }
      this.selectedSeats.set([]);
    }

    if (this.selectedSeats().length >= this.passengers().length) {
      this.toastService.showError(`You can only select up to ${this.passengers().length} seats for your ${this.passengers().length} passengers.`);
      return;
    }

    this.seatClass.set(cls);
    this.selectedSeats.update(s => [...s, seat]);
  }

  getPrice(): number {
    const f = this.flight();
    if (!f) return 420;
    switch (this.seatClass()) {
      case 'First': return f.firstClassPrice || 1240;
      case 'Business': return f.businessPrice || 840;
      default: return f.economyPrice || 420;
    }
  }

  getSeatPrice(seat: string): number {
    let base = this.getPrice();
    if (seat.endsWith('A') || seat.endsWith('F')) {
      base += 200; // Window seat surcharge
    }
    return base;
  }

  getSeatsTotal(): number {
    const paxCount = this.passengers().length;
    let seatsTotal = 0;
    
    if (this.selectedSeats().length > 0) {
      seatsTotal = this.selectedSeats().reduce((sum, seat) => sum + this.getSeatPrice(seat), 0);
      const remainingPax = paxCount - this.selectedSeats().length;
      seatsTotal += remainingPax * this.getPrice();
    } else {
      seatsTotal = paxCount * this.getPrice();
    }
    return seatsTotal;
  }

  getTotalPrice(): number {
    const paxCount = this.passengers().length;
    return this.getSeatsTotal() + (this.baggageWeight() > 7 ? 45 : 0) + (84.50 * paxCount);
  }

  addPassenger() {
    this.passengers.update(p => [...p, { name: '', age: 25, gender: 'Male', aadharCardNo: '' }]);
  }

  removePassenger(index: number) {
    if (this.passengers().length > 1) {
      this.passengers.update(p => p.filter((_, i) => i !== index));
    }
  }

  confirmBooking() {
    if (!this.flight()) return;
    
    // Validate passengers
    if (this.passengers().some(p => !p.name || !p.age || !p.gender || p.aadharCardNo.length !== 12)) {
      this.toastService.showError("Please check passenger details. Make sure Aadhar Card Number is 12 digits.");
      return;
    }

    if (this.selectedSeats().length !== this.passengers().length) {
      this.toastService.showError(`Please select ${this.passengers().length} seats to continue.`);
      return;
    }

    this.loading.set(true);
    
    // 1. Create the base booking
    this.bookingService.createBooking({
      userId: this.authService.userId(),
      flightId: this.flight()!.id,
      scheduleId: this.scheduleId() ?? undefined,
      seatClass: this.seatClass(),
      baggageWeight: this.baggageWeight(),
      passengerCount: this.passengers().length,
      totalAmount: this.getTotalPrice()
    }).subscribe({
      next: (booking) => {
        // Map selected seats sequentially to passengers
        const mappedPassengers = this.passengers().map((p, i) => ({
          ...p,
          seatNumber: this.selectedSeats()[i] || undefined
        }));

        // 2. Add the passengers to the newly created booking
        this.bookingService.addPassengers(booking.id, mappedPassengers).subscribe({
          next: () => {
            this.loading.set(false);
            this.toastService.showSuccess("Passenger details confirmed!");
            const rolePrefix = this.authService.userRole() === 'Dealer' ? 'dealer' : 'passenger';
            this.router.navigate([`/${rolePrefix}/payment`, booking.id]);
          },
          error: (err) => {
            this.loading.set(false);
            console.error("Error adding passengers:", err);
            const msg = err.error?.message || "Error adding passengers to booking.";
            this.toastService.showError(msg);
          }
        });
      },
      error: (err) => {
        this.loading.set(false);
        console.error("Error creating booking:", err);
      }
    });
  }
}
