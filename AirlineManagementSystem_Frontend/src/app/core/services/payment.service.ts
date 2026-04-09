import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Payment, ProcessPaymentRequest, VerifyPaymentRequest } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly apiUrl = `${environment.apiBaseUrl}/payments`;

  constructor(private http: HttpClient) {}

  processPayment(data: ProcessPaymentRequest): Observable<Payment> {
    return this.http.post<Payment>(`${this.apiUrl}/process`, data);
  }

  createOrder(bookingId: number, amount: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/create-order`, { bookingId, amount });
  }

  verifyPayment(data: VerifyPaymentRequest): Observable<Payment> {
    return this.http.post<Payment>(`${this.apiUrl}/verify`, data);
  }

  getPayment(id: number): Observable<Payment> {
    return this.http.get<Payment>(`${this.apiUrl}/${id}`);
  }

  refundPayment(id: number): Observable<Payment> {
    return this.http.post<Payment>(`${this.apiUrl}/${id}/refund`, {});
  }
}
