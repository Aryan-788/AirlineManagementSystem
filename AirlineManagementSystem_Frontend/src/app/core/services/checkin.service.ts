import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CheckIn, CheckInRequest, BoardingPass } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class CheckInService {
  private readonly apiUrl = `${environment.apiBaseUrl}/checkin`;

  constructor(private http: HttpClient) {}

  onlineCheckIn(data: CheckInRequest, passengerName: string, flightNumber: string, flightId: number, departureTime: string): Observable<CheckIn> {
    const params = new HttpParams()
      .set('passengerName', passengerName)
      .set('flightNumber', flightNumber)
      .set('flightId', flightId.toString())
      .set('departureTime', departureTime);
    return this.http.post<CheckIn>(`${this.apiUrl}/online`, data, { params });
  }

  getCheckIn(id: number): Observable<CheckIn> {
    return this.http.get<CheckIn>(`${this.apiUrl}/${id}`);
  }

  getBoardingPass(id: number): Observable<BoardingPass> {
    return this.http.get<BoardingPass>(`${this.apiUrl}/${id}/boarding-pass`);
  }

  getSummary(): Observable<import('../models/api.models').CheckInSummary> {
    return this.http.get<import('../models/api.models').CheckInSummary>(`${this.apiUrl}/summary`);
  }
}
