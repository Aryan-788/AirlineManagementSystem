import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class TimezoneService {
  // We store the timezone string here. UTC or Asia/Kolkata
  private timezoneSubject = new BehaviorSubject<string>('UTC');
  public timezone$ = this.timezoneSubject.asObservable();

  constructor() {
    const savedTimezone = localStorage.getItem('selectedTimezone');
    if (savedTimezone) {
      this.timezoneSubject.next(savedTimezone);
    }
  }

  public setTimezone(timezone: string): void {
    localStorage.setItem('selectedTimezone', timezone);
    this.timezoneSubject.next(timezone);
  }

  public getTimezone(): string {
    return this.timezoneSubject.value;
  }
}
