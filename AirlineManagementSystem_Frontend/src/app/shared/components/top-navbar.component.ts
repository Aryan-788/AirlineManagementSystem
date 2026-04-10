import { Component, Input, OnInit, signal, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { Notification } from '../../core/models/api.models';
import { ProfileModalComponent } from './profile-modal.component';
import { TimezoneService } from '../../core/services/timezone.service';
import { AirlineTimePipe } from '../pipes/airline-time.pipe';

@Component({
  selector: 'app-top-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule, ProfileModalComponent, AirlineTimePipe],
  template: `
    <nav class="top-nav glass-nav">
      <div class="nav-container">
        <div class="nav-left">
          <span class="brand font-headline" routerLink="/">SkyLedger</span>
          <div class="nav-links" *ngIf="showLinks">
            <a [routerLink]="getDashboardLink()" routerLinkActive="active">Dashboard</a>
            <a *ngIf="authService.userRole() === 'Admin'" routerLink="/admin/flights" routerLinkActive="active">Flight Control</a>
            <a *ngIf="authService.userRole() === 'Passenger'" routerLink="/passenger/search" routerLinkActive="active">Search Flights</a>
          </div>
        </div>
        <div class="nav-right" *ngIf="authService.isLoggedIn()">
          <div class="search-box" *ngIf="showSearch">
            <span class="material-symbols-outlined">search</span>
            <input type="text" placeholder="Search flights..." />
          </div>
          <div class="notif-wrapper">
            <button class="icon-btn" title="Notifications" (click)="toggleNotifications(); $event.stopPropagation()">
              <span class="material-symbols-outlined">notifications</span>
              <span class="badge" *ngIf="unreadCount() > 0">{{ unreadCount() }}</span>
            </button>
            <div class="notif-dropdown shadow-lg" *ngIf="showNotifications()" (click)="$event.stopPropagation()">
              <div class="notif-header">
                <span class="notif-title">Notifications</span>
                <button class="mark-all" (click)="markAllAsRead()" *ngIf="unreadCount() > 0">Mark all as read</button>
              </div>
              <div class="notif-list">
                <div class="notif-item" *ngFor="let n of notifications()" [class.unread]="!n.isRead">
                  <div class="notif-dot" *ngIf="!n.isRead"></div>
                  <div class="notif-content">
                    <div class="notif-subject">{{ n.subject }}</div>
                    <div class="notif-message">{{ n.message }}</div>
                    <div class="notif-time">{{ n.createdAt | airlineTime:'medium' }}</div>
                  </div>
                </div>
                <div class="notif-empty" *ngIf="notifications().length === 0">
                  <span class="material-symbols-outlined">notifications_off</span>
                  <p>No notifications yet</p>
                </div>
              </div>
            </div>
          </div>
          <button class="icon-btn" title="Settings">
            <span class="material-symbols-outlined">settings</span>
          </button>
          
          <div class="tz-selector">
            <span class="material-symbols-outlined tz-icon">public</span>
            <select (change)="onTimezoneChange($event)" [value]="timezoneService.getTimezone()">
              <option value="UTC">UTC</option>
              <option value="IST">IST (India)</option>
              <option value="EST">EST (US)</option>
            </select>
          </div>

          <div class="user-profile-wrapper">
            <div class="user-avatar" (click)="toggleProfileMenu(); $event.stopPropagation()">
              <div class="avatar-circle">{{ getInitials() }}</div>
            </div>
            
            <div class="profile-dropdown shadow-lg" *ngIf="showProfileMenu()" (click)="$event.stopPropagation()">
              <div class="profile-header">
                <div class="ph-name">{{ authService.userName() }}</div>
                <div class="ph-role">{{ authService.userRole() }}</div>
              </div>
              <div class="profile-links">
                <button (click)="openProfileModal(); showProfileMenu.set(false)" class="pl-btn">
                  <span class="material-symbols-outlined">person</span>
                  My Profile
                </button>
                <button (click)="authService.logout(); showProfileMenu.set(false)" class="pl-btn text-error">
                  <span class="material-symbols-outlined">logout</span>
                  Logout
                </button>
              </div>
            </div>
          </div>
        </div>
        <div class="nav-right" *ngIf="!authService.isLoggedIn()">
          <button routerLink="/login" class="login-btn">Login</button>
        </div>
      </div>
      <div class="nav-divider"></div>
    </nav>
    <app-profile-modal [isOpen]="isProfileModalOpen()" (close)="isProfileModalOpen.set(false)"></app-profile-modal>
  `,
  styles: [`
    .top-nav {
      position: fixed;
      top: 0;
      width: 100%;
      z-index: 50;
      box-shadow: var(--shadow-sm);
    }
    .nav-container {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0 2rem;
      height: 64px;
      max-width: 1920px;
      margin: 0 auto;
    }
    .nav-left {
      display: flex;
      align-items: center;
      gap: 2rem;
    }
    .brand {
      font-size: 1.25rem;
      font-weight: 700;
      letter-spacing: -0.025em;
      color: #172554;
      cursor: pointer;
    }
    .nav-links {
      display: flex;
      gap: 1.5rem;
      align-items: center;
    }
    .nav-links a {
      color: #64748b;
      text-decoration: none;
      font-size: 0.875rem;
      font-weight: 500;
      transition: color 0.2s;
      padding: 1.25rem 0;
    }
    .nav-links a:hover { color: #2563eb; }
    .nav-links a.active {
      color: #1d4ed8;
      font-weight: 600;
      border-bottom: 2px solid #1d4ed8;
    }
    .nav-right {
      display: flex;
      align-items: center;
      gap: 1rem;
    }
    .search-box {
      display: flex;
      align-items: center;
      background: #f1f5f9;
      padding: 0.375rem 0.75rem;
      border-radius: 8px;
      gap: 0.5rem;
    }
    .search-box span { color: #64748b; font-size: 1rem; }
    .search-box input {
      border: none;
      background: transparent;
      font-size: 0.875rem;
      width: 160px;
      color: var(--on-surface);
    }
    .search-box input:focus { outline: none; }
    .icon-btn {
      position: relative;
      background: none;
      border: none;
      cursor: pointer;
      padding: 0.5rem;
      border-radius: 50%;
      transition: background 0.2s;
      color: #172554;
    }
    .icon-btn:hover { background: #f1f5f9; }
    .badge {
      position: absolute;
      top: -2px;
      right: -2px;
      background: var(--error);
      color: white;
      font-size: 9px;
      font-weight: 700;
      padding: 2px 5px;
      border-radius: 999px;
      min-width: 16px;
      text-align: center;
      border: 2px solid white;
    }
    .notif-wrapper { position: relative; }
    .notif-dropdown {
      position: absolute;
      top: 100%;
      right: 0;
      width: 320px;
      background: white;
      border-radius: 12px;
      margin-top: 0.5rem;
      border: 1px solid var(--outline-variant);
      z-index: 100;
      overflow: hidden;
    }
    .notif-header {
      padding: 1rem;
      border-bottom: 1px solid #f1f5f9;
      display: flex;
      justify-content: space-between;
      align-items: center;
      background: #f8fafc;
    }
    .notif-title { font-weight: 700; color: var(--primary); font-size: 0.875rem; }
    .mark-all {
      font-size: 0.75rem;
      color: var(--primary);
      background: none;
      border: none;
      cursor: pointer;
      font-weight: 600;
    }
    .mark-all:hover { text-decoration: underline; }
    .notif-list { max-height: 380px; overflow-y: auto; }
    .notif-item {
      padding: 1rem;
      display: flex;
      gap: 0.75rem;
      border-bottom: 1px solid #f8fafc;
      transition: background 0.2s;
    }
    .notif-item:hover { background: #f8fafc; }
    .notif-item.unread { background: #eff6ff; }
    .notif-dot {
      width: 8px;
      height: 8px;
      background: #3b82f6;
      border-radius: 50%;
      flex-shrink: 0;
      margin-top: 4px;
    }
    .notif-content { flex: 1; }
    .notif-subject { font-weight: 700; font-size: 0.8125rem; color: var(--on-surface); margin-bottom: 2px; }
    .notif-message { font-size: 0.75rem; color: var(--on-surface-variant); line-height: 1.4; margin-bottom: 4px; }
    .notif-time { font-size: 0.6875rem; color: #94a3b8; }
    .notif-empty { padding: 3rem 1rem; text-align: center; color: #94a3b8; }
    .notif-empty span { font-size: 2rem; margin-bottom: 0.5rem; display: block; opacity: 0.5; }
    .notif-empty p { font-size: 0.875rem; }
    .user-avatar { cursor: pointer; margin-left: 0.5rem; }
    .avatar-circle {
      width: 36px;
      height: 36px;
      border-radius: 50%;
      background: linear-gradient(135deg, var(--primary), var(--primary-container));
      color: white;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 0.75rem;
      font-weight: 700;
      border: 2px solid var(--outline-variant);
      transition: transform 0.2s;
    }
    .avatar-circle:hover { transform: scale(1.05); }
    .login-btn {
      background: #1d4ed8;
      color: white;
      border: none;
      padding: 0.5rem 1.5rem;
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;
      transition: background 0.2s;
    }
    .login-btn:hover { background: #1e3a8a; }
    .nav-divider {
      height: 1px;
      width: 100%;
      background: #f1f5f9;
    }
    .tz-selector {
      display: flex;
      align-items: center;
      background: #f8fafc;
      padding: 0.25rem 0.5rem;
      border-radius: 8px;
      gap: 0.5rem;
      border: 1px solid #e2e8f0;
      margin-right: 0.5rem;
    }
    .tz-icon { font-size: 1.1rem; color: #1d4ed8; }
    .tz-selector select {
      border: none;
      background: transparent;
      font-size: 0.8rem;
      font-weight: 600;
      color: #334155;
      cursor: pointer;
      outline: none;
    }
    .tz-selector select:hover { color: #1d4ed8; }
    .user-profile-wrapper { position: relative; display: flex; align-items: center; }
    .profile-dropdown {
      position: absolute;
      top: calc(100% + 12px);
      right: 0;
      width: 220px;
      background: white;
      border-radius: 12px;
      border: 1px solid var(--outline-variant);
      z-index: 100;
      overflow: hidden;
      box-shadow: 0 10px 25px rgba(0,0,0,0.1);
    }
    .profile-header {
      padding: 1rem;
      border-bottom: 1px solid #f1f5f9;
      background: #f8fafc;
    }
    .ph-name { font-weight: 700; color: #0f172a; font-size: 0.875rem; }
    .ph-role { font-size: 0.75rem; color: #64748b; margin-top: 2px; }
    .profile-links { padding: 0.5rem; display: flex; flex-direction: column; }
    .pl-btn {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      width: 100%;
      padding: 0.75rem 1rem;
      background: none;
      border: none;
      text-align: left;
      font-size: 0.875rem;
      font-weight: 500;
      color: #334155;
      border-radius: 8px;
      cursor: pointer;
      transition: background 0.2s;
    }
    .pl-btn:hover { background: #f1f5f9; }
    .pl-btn span { font-size: 1.25rem; }
    .text-error { color: var(--error) !important; }
    .text-error:hover { background: #fef2f2 !important; }
    @media (max-width: 768px) {
      .nav-links, .search-box { display: none; }
    }
  `]
})
export class TopNavbarComponent {
  @Input() showSearch = true;
  @Input() showLinks = true;

  notifications = signal<Notification[]>([]);
  showNotifications = signal(false);
  showProfileMenu = signal(false);
  isProfileModalOpen = signal(false);

  unreadCount = signal(0);

  constructor(
    public authService: AuthService,
    private notificationService: NotificationService,
    public timezoneService: TimezoneService
  ) {}

  ngOnInit() {
    if (this.authService.isLoggedIn()) {
      this.fetchNotifications();
    }
  }

  fetchNotifications() {
    const userId = this.authService.userId();
    if (userId) {
      this.notificationService.getUserNotifications(userId).subscribe({
        next: (data) => {
          this.notifications.set(data.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()));
          this.unreadCount.set(data.filter(n => !n.isRead).length);
        }
      });
    }
  }

  toggleNotifications() {
    this.showNotifications.update(s => !s);
    if (this.showNotifications() && this.unreadCount() > 0) {
      // Logic to mark as read when opened (or user can clear all)
    }
  }

  markAllAsRead() {
    const userId = this.authService.userId();
    if (userId) {
      this.notificationService.markAllAsRead(userId).subscribe({
        next: () => {
          this.notifications.update(list => list.map(n => ({ ...n, isRead: true })));
          this.unreadCount.set(0);
        }
      });
    }
  }

  @HostListener('document:click')
  clickout() {
    this.showNotifications.set(false);
    this.showProfileMenu.set(false);
  }

  toggleProfileMenu() {
    this.showProfileMenu.update(s => !s);
    if (this.showProfileMenu()) {
      this.showNotifications.set(false);
    }
  }

  openProfileModal() {
    this.isProfileModalOpen.set(true);
  }

  getInitials(): string {
    const name = this.authService.userName();
    if (!name) return '??';
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }

  getDashboardLink(): string {
    return this.authService.getDefaultRoute();
  }

  onTimezoneChange(event: any) {
    this.timezoneService.setTimezone(event.target.value);
  }
}
