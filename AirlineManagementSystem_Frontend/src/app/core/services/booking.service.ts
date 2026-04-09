import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Booking, CreateBookingRequest, Passenger, CreatePassengerRequest, ScheduleBooking } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class BookingService {
  private readonly apiUrl = `${environment.apiBaseUrl}/bookings`;

  constructor(private http: HttpClient) {}

  createBooking(data: CreateBookingRequest): Observable<Booking> {
    return this.http.post<Booking>(this.apiUrl, data);
  }

  addPassengers(bookingId: number, passengers: CreatePassengerRequest[]): Observable<Passenger[]> {
    return this.http.post<Passenger[]>(`${this.apiUrl}/${bookingId}/passengers`, passengers);
  }

  getPassengers(bookingId: number): Observable<Passenger[]> {
    return this.http.get<Passenger[]>(`${this.apiUrl}/${bookingId}/passengers`);
  }

  getBooking(id: number): Observable<Booking> {
    return this.http.get<Booking>(`${this.apiUrl}/${id}`);
  }

  getBookingByPnr(pnr: string): Observable<Booking> {
    return this.http.get<Booking>(`${this.apiUrl}/pnr/${pnr}`);
  }

  getBookingHistory(userId: number): Observable<Booking[]> {
    return this.http.get<Booking[]>(`${this.apiUrl}/history/${userId}`);
  }

  getBookingsBySchedule(scheduleId: number): Observable<ScheduleBooking[]> {
    return this.http.get<ScheduleBooking[]>(`${this.apiUrl}/schedule/${scheduleId}`);
  }

  getUserBookings(userId: number): Observable<Booking[]> {
    return this.http.get<Booking[]>(`${this.apiUrl}/history/${userId}`);
  }

  cancelBooking(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/cancel`, {});
  }

  cancelPassenger(passengerId: number, reason: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/passengers/${passengerId}/cancel`, { cancellationReason: reason });
  }

  getOccupiedSeats(flightId: number, scheduleId?: number): Observable<string[]> {
    let url = `${this.apiUrl}/occupied-seats?flightId=${flightId}`;
    if (scheduleId) url += `&scheduleId=${scheduleId}`;
    return this.http.get<string[]>(url);
  }
}
