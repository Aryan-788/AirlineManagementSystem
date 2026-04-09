import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { TopNavbarComponent } from '../../../shared/components/top-navbar.component';
import { BookingService } from '../../../core/services/booking.service';
import { PaymentService } from '../../../core/services/payment.service';
import { FlightService } from '../../../core/services/flight.service';
import { AgentService } from '../../../core/services/agent.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { Booking, Payment, Flight } from '../../../core/models/api.models';

declare var Razorpay: any;

@Component({
  selector: 'app-payment',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TopNavbarComponent],
  templateUrl: './payment.html',
  styleUrl: './payment.css'
})
export class PaymentComponent implements OnInit {
  booking = signal<Booking | null>(null);
  flight = signal<Flight | null>(null);
  loading = signal(false);
  paymentSuccess = signal(false);
  payment = signal<Payment | null>(null);

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private bookingService: BookingService,
    private flightService: FlightService,
    private paymentService: PaymentService,
    private agentService: AgentService,
    public authService: AuthService,
    private toastService: ToastService
  ) {}

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.bookingService.getBooking(id).subscribe({
      next: (b) => {
        this.booking.set(b);
        this.flightService.getFlightById(b.flightId).subscribe(f => this.flight.set(f));
      },
      error: () => this.router.navigate([this.authService.getDefaultRoute()])
    });
  }


  processPayment() {
    if (!this.booking()) return;

    // Check if Razorpay SDK is loaded
    if (typeof Razorpay === 'undefined') {
      this.toastService.showError('Razorpay SDK not loaded. Please refresh the page and try again.');
      console.error('Razorpay SDK is not loaded. Check if checkout.js script is included in index.html.');
      return;
    }

    this.loading.set(true);

    const amt = this.booking()?.totalAmount || 0;
    console.log('[Payment] Creating order for bookingId:', this.booking()!.id, 'amount:', amt);

    this.paymentService.createOrder(this.booking()!.id, amt).subscribe({
      next: (orderData: any) => {
        console.log('[Payment] Order created successfully:', orderData);

        const options = {
          key: orderData.key,               // Use key from backend response
          amount: orderData.amount,          // Use amount (in paise) from backend response
          currency: orderData.currency,      // Use currency from backend response
          name: 'SkyLedger Airlines',
          description: 'Flight Booking Payment',
          order_id: orderData.orderId,
          handler: (response: any) => {
            console.log('[Payment] Razorpay payment success callback:', response);
            this.verifyPayment(response);
          },
          prefill: {
            name: this.authService.userName(),
            email: this.authService.userEmail(),
          },
          theme: { color: '#000c40' },
          modal: {
            ondismiss: () => {
              console.log('[Payment] Razorpay checkout dismissed by user');
              this.loading.set(false);
            }
          }
        };

        console.log('[Payment] Opening Razorpay checkout with options:', { ...options, key: '***' });
        const rzp = new Razorpay(options);
        rzp.on('payment.failed', (failResponse: any) => {
          console.error('[Payment] Razorpay payment failed:', failResponse.error);
          this.loading.set(false);
          this.toastService.showError('Payment failed: ' + (failResponse.error?.description || 'Unknown error'));
        });
        rzp.open();
      },
      error: (err: any) => {
        console.error('[Payment] Failed to create order:', err);
        this.loading.set(false);
        this.toastService.showError('Failed to create payment order. Please try again.');
      }
    });
  }

  verifyPayment(response: any) {
    console.log('[Payment] Verifying payment signature...');
    this.paymentService.verifyPayment({
      bookingId: this.booking()!.id,
      amount: this.booking()?.totalAmount || 0,
      razorpayOrderId: response.razorpay_order_id,
      razorpayPaymentId: response.razorpay_payment_id,
      razorpaySignature: response.razorpay_signature,
      userId: this.authService.userId()
    }).subscribe({
      next: (p) => {
        console.log('[Payment] Payment verified successfully:', p);
        this.payment.set(p);
        this.paymentSuccess.set(true);
        this.loading.set(false);
        this.toastService.showSuccess('Payment Successful! Your journey is confirmed.');
        const b = this.booking();
        if (b) {
          this.booking.set({ ...b, status: 'Confirmed', paymentStatus: 'Success' });
        }

        // Auto-record dealer commission if user is a Dealer
        if (this.authService.userRole() === 'Dealer' && b) {
          this.recordDealerCommission(b);
        }
      },
      error: (err: any) => {
        console.error('[Payment] Signature verification failed:', err);
        this.loading.set(false);
        this.toastService.showError('Payment verification failed. Please contact support.');
      }
    });
  }

  private recordDealerCommission(booking: Booking) {
    const email = this.authService.userEmail();
    console.log('[Payment] Recording dealer commission for email:', email);

    // Look up dealer ID by email, then record the booking commission
    this.agentService.getDealerByEmail(email).subscribe({
      next: (dealer) => {
        console.log('[Payment] Found dealer:', dealer.id, dealer.dealerName);
        this.agentService.recordBooking(
          dealer.id,
          booking.id,
          booking.flightId,
          booking.totalAmount
        ).subscribe({
          next: () => {
            console.log('[Payment] Dealer commission recorded successfully');
            this.toastService.showSuccess('Commission recorded to your dealer account!');
          },
          error: (err) => {
            console.error('[Payment] Failed to record commission:', err);
            this.toastService.showError('Commission recording failed. Contact admin.');
          }
        });
      },
      error: (err) => {
        console.error('[Payment] Dealer lookup failed for email:', email, err);
        this.toastService.showError('Dealer account not found. Commission not recorded.');
      }
    });
  }
}
