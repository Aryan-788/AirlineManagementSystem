import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { BookingService } from '../../../core/services/booking.service';
import { CheckInService } from '../../../core/services/checkin.service';
import { FlightService } from '../../../core/services/flight.service';
import { AuthService } from '../../../core/services/auth.service';
import { Booking, Passenger, BoardingPass } from '../../../core/models/api.models';
import { TopNavbarComponent } from '../../../shared/components/top-navbar.component';

@Component({
  selector: 'app-check-in',
  standalone: true,
  imports: [CommonModule, RouterModule, TopNavbarComponent],
  templateUrl: './check-in.html',
  styleUrl: './check-in.css'
})
export class CheckInComponent implements OnInit {
  bookings = signal<Booking[]>([]);
  loading = signal(true);
  processingIds = signal<Set<number>>(new Set());
  
  // Boarding Pass display
  showBoardingPass = signal(false);
  currentBoardingPass = signal<BoardingPass | null>(null);

  constructor(
    private bookingService: BookingService,
    private checkInService: CheckInService,
    private flightService: FlightService,
    private authService: AuthService
  ) {}

  ngOnInit() {
    this.loadBookings();
  }

  loadBookings() {
    this.loading.set(true);
    const userId = this.authService.userId();
    if (userId) {
      this.bookingService.getUserBookings(userId).subscribe({
        next: (data) => {
          // Filter for confirmed bookings
          this.bookings.set(data.filter(b => b.status === 'Confirmed' || b.status === 'CONFIRMED'));
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
    }
  }

  isProcessing(id: number): boolean {
    return this.processingIds().has(id);
  }

  performCheckIn(booking: Booking) {
    if (this.isProcessing(booking.id)) return;

    // Start processing for this ID
    this.processingIds.update(ids => {
      const newIds = new Set(ids);
      newIds.add(booking.id);
      return newIds;
    });
    
    // 1. Get passengers and 2. Get flight details
    this.bookingService.getPassengers(booking.id).subscribe({
      next: (passengers: Passenger[]) => {
        if (passengers.length === 0) {
          this.stopProcessing(booking.id);
          alert('No passengers found for this booking.');
          return;
        }

        this.flightService.getFlightById(booking.flightId).subscribe({
          next: (flight) => {
            const p = passengers[0];
            const userId = this.authService.userId();

            if (userId === null) {
              this.stopProcessing(booking.id);
              alert('User session not found.');
              return;
            }
            
            // 3. Perform online check-in with REAL data
            this.checkInService.onlineCheckIn({
              bookingId: booking.id,
              userId: userId,
              seatNumber: p.seatNumber || undefined
            }, p.name, flight.flightNumber, booking.flightId, flight.departureTime).subscribe({
              next: (res) => {
                // 4. Get the boarding pass
                this.checkInService.getBoardingPass(res.id).subscribe({
                  next: (bp) => {
                    this.currentBoardingPass.set(bp);
                    this.showBoardingPass.set(true);
                    this.stopProcessing(booking.id);
                  },
                  error: () => this.stopProcessing(booking.id)
                });
              },
              error: (err) => {
                console.error('Check-in failed:', err);
                const msg = err.error?.message || 'Check-in failed. Please try again.';
                alert(msg);
                this.stopProcessing(booking.id);
              }
            });
          },
          error: () => {
            this.stopProcessing(booking.id);
            alert('Could not retrieve flight details.');
          }
        });
      },
      error: () => this.stopProcessing(booking.id)
    });
  }

  private stopProcessing(id: number) {
    this.processingIds.update(ids => {
      const newIds = new Set(ids);
      newIds.delete(id);
      return newIds;
    });
  }

  closeBoardingPass() {
    this.showBoardingPass.set(false);
    this.currentBoardingPass.set(null);
  }
}
