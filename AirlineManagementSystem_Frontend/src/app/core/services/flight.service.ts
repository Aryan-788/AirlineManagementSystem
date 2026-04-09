import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Flight, CreateFlightRequest } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class FlightService {
  private readonly apiUrl = `${environment.apiBaseUrl}/flights`;

  constructor(private http: HttpClient) {}

  createFlight(data: CreateFlightRequest): Observable<Flight> {
    return this.http.post<Flight>(this.apiUrl, data);
  }

  searchFlights(source: string, destination: string, departureDate: string): Observable<Flight[]> {
    const params = new HttpParams()
      .set('source', source)
      .set('destination', destination)
      .set('departureDate', departureDate);
    return this.http.get<Flight[]>(`${this.apiUrl}/search`, { params });
  }

  getAllFlights(): Observable<Flight[]> {
    return this.http.get<Flight[]>(this.apiUrl);
  }

  getFlightById(id: number): Observable<Flight> {
    return this.http.get<Flight>(`${this.apiUrl}/${id}`);
  }

  updateFlight(id: number, data: Partial<Flight>): Observable<Flight> {
    return this.http.put<Flight>(`${this.apiUrl}/${id}`, data);
  }

  deleteFlight(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  delayFlight(id: number, newDepartureTime: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/delay`, JSON.stringify(newDepartureTime), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  cancelFlight(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/cancel`, {});
  }

  assignGate(id: number, gate: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/assign-gate`, JSON.stringify(gate), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  assignAircraft(id: number, aircraft: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/assign-aircraft`, JSON.stringify(aircraft), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  assignCrew(id: number, crew: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/assign-crew`, JSON.stringify(crew), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  // --- Schedule Endpoints ---
  createSchedule(data: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/schedules`, data);
  }

  getAllSchedules(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/schedules`);
  }

  getScheduleById(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/schedules/${id}`);
  }

  searchSchedules(source: string, destination: string, departureDate: string): Observable<any[]> {
    const params = new HttpParams()
      .set('source', source)
      .set('destination', destination)
      .set('departureDate', departureDate);
    return this.http.get<any[]>(`${this.apiUrl}/schedules/search`, { params });
  }

  getSchedulesByFlightId(flightId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${flightId}/schedules`);
  }

  updateSchedule(id: number, data: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/schedules/${id}`, data);
  }

  deleteSchedule(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/schedules/${id}`);
  }

  cancelSchedule(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/schedules/${id}/cancel`, {});
  }
}
