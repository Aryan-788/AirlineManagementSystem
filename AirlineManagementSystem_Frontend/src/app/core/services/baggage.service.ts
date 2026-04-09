import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Baggage } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class BaggageService {
  private readonly apiUrl = `${environment.apiBaseUrl}/baggage`;

  constructor(private http: HttpClient) {}

  addBaggage(bookingId: number, weight: number, passengerName: string, flightNumber: string): Observable<Baggage> {
    return this.http.post<Baggage>(this.apiUrl, { bookingId, weight, passengerName, flightNumber });
  }

  getAllBaggage(): Observable<Baggage[]> {
    return this.http.get<Baggage[]>(this.apiUrl);
  }

  getBaggage(id: number): Observable<Baggage> {
    return this.http.get<Baggage>(`${this.apiUrl}/${id}`);
  }

  updateBaggageStatus(id: number, status: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}/status`, { status });
  }

  markDelivered(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/deliver`, {});
  }

  trackBaggage(trackingNumber: string): Observable<Baggage> {
    return this.http.get<Baggage>(`${this.apiUrl}/track/${trackingNumber}`);
  }

  getBaggageByBooking(bookingId: number): Observable<Baggage[]> {
    return this.http.get<Baggage[]>(`${this.apiUrl}/booking/${bookingId}`);
  }

  getSummary(): Observable<import('../models/api.models').BaggageSummary> {
    return this.http.get<import('../models/api.models').BaggageSummary>(`${this.apiUrl}/summary`);
  }
}
