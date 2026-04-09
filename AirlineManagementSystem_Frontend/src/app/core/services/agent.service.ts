import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Dealer, DealerBooking, CommissionReport } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class AgentService {
  private readonly apiUrl = `${environment.apiBaseUrl}/agent`;

  constructor(private http: HttpClient) {}

  createDealer(data: { dealerName: string; dealerEmail: string; allocatedSeats: number; commissionRate: number }): Observable<Dealer> {
    return this.http.post<Dealer>(`${this.apiUrl}/dealer`, data);
  }

  getDealer(id: number): Observable<Dealer> {
    return this.http.get<Dealer>(`${this.apiUrl}/dealer/${id}`);
  }

  getAllDealers(): Observable<Dealer[]> {
    return this.http.get<Dealer[]>(`${this.apiUrl}/dealers`);
  }

  allocateSeats(dealerId: number, seats: number): Observable<any> {
    const params = new HttpParams().set('seats', seats.toString());
    return this.http.post(`${this.apiUrl}/dealer/${dealerId}/allocate-seats`, {}, { params });
  }

  recordBooking(dealerId: number, bookingId: number, flightId: number, bookingAmount: number): Observable<DealerBooking> {
    const params = new HttpParams()
      .set('dealerId', dealerId.toString())
      .set('bookingId', bookingId.toString())
      .set('flightId', flightId.toString())
      .set('bookingAmount', bookingAmount.toString());
    return this.http.post<DealerBooking>(`${this.apiUrl}/booking/record`, {}, { params });
  }

  getCommissionReport(): Observable<CommissionReport[]> {
    return this.http.get<CommissionReport[]>(`${this.apiUrl}/commission-report`);
  }

  getDealerPerformance(dealerId: number): Observable<import('../models/api.models').DealerPerformance> {
    return this.http.get<import('../models/api.models').DealerPerformance>(`${this.apiUrl}/dealer/${dealerId}/performance`);
  }

  getDealerByEmail(email: string): Observable<Dealer> {
    const params = new HttpParams().set('email', email);
    return this.http.get<Dealer>(`${this.apiUrl}/dealer/by-email`, { params });
  }
}
