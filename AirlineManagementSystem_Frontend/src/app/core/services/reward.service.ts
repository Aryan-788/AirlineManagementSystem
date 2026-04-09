import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Reward, RewardBalance } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class RewardService {
  private readonly apiUrl = `${environment.apiBaseUrl}/reward`;

  constructor(private http: HttpClient) {}

  earnPoints(userId: number, points: number, transactionType: string, bookingId?: number): Observable<Reward> {
    const params = new HttpParams()
      .set('transactionType', transactionType)
      .set('bookingId', (bookingId ?? 0).toString());
    return this.http.post<Reward>(`${this.apiUrl}/earn`, { userId, points, transactionType }, { params });
  }

  getBalance(userId: number): Observable<RewardBalance> {
    return this.http.get<RewardBalance>(`${this.apiUrl}/${userId}/balance`);
  }

  getHistory(userId: number): Observable<Reward[]> {
    return this.http.get<Reward[]>(`${this.apiUrl}/${userId}/history`);
  }

  redeemPoints(userId: number, points: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/redeem`, { userId, points });
  }
}
