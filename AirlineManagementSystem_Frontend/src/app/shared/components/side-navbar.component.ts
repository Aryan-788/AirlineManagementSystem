import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

interface NavItem {
  icon: string;
  label: string;
  route: string;
}

@Component({
  selector: 'app-side-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <aside class="side-nav custom-scrollbar">
      <div class="nav-header">
        <div class="brand-block">
          <div class="brand-icon">
            <span class="material-symbols-outlined">flight_takeoff</span>
          </div>
          <div>
            <h1 class="brand-name font-headline">SkyLedger</h1>
            <p class="brand-sub">{{ subtitle }}</p>
          </div>
        </div>
        <button class="new-flight-btn" *ngIf="authService.userRole() === 'Admin'" routerLink="/admin/flights">
          <span class="material-symbols-outlined">add</span>
          New Flight
        </button>
      </div>

      <nav class="nav-items">
         <a *ngFor="let item of navItems"
           [routerLink]="item.route"
           routerLinkActive="active"
           [routerLinkActiveOptions]="{exact: item.route === '/admin/dashboard'}"
           class="nav-item">
          <span class="material-symbols-outlined">{{ item.icon }}</span>
          <span>{{ item.label }}</span>
        </a>
      </nav>

      <div class="nav-footer">
        <a class="nav-item" href="javascript:void(0)">
          <span class="material-symbols-outlined">help_outline</span>
          <span>Support</span>
        </a>
        <a class="nav-item logout" (click)="authService.logout()">
          <span class="material-symbols-outlined">logout</span>
          <span>Logout</span>
        </a>
      </div>
    </aside>
  `,
  styles: [`
    .side-nav {
      height: 100vh;
      width: 256px;
      position: fixed;
      left: 0;
      top: 0;
      background: #f8fafc;
      display: flex;
      flex-direction: column;
      padding: 1.5rem 0;
      gap: 0.5rem;
      z-index: 40;
      overflow-y: auto;
    }
    .nav-header {
      padding: 0 1.5rem;
      margin-bottom: 2rem;
    }
    .brand-block {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      margin-bottom: 1.5rem;
    }
    .brand-icon {
      width: 40px;
      height: 40px;
      background: var(--primary);
      border-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
      color: white;
      box-shadow: var(--shadow-lg);
    }
    .brand-icon span { font-size: 1.25rem; }
    .brand-name {
      font-size: 1rem;
      font-weight: 700;
      color: #172554;
      line-height: 1;
    }
    .brand-sub {
      font-size: 10px;
      text-transform: uppercase;
      letter-spacing: 0.1em;
      color: #94a3b8;
      font-weight: 700;
      margin-top: 4px;
    }
    .new-flight-btn {
      width: 100%;
      background: linear-gradient(135deg, var(--primary), var(--primary-container));
      color: white;
      padding: 0.75rem;
      border: none;
      border-radius: 8px;
      font-weight: 600;
      font-size: 0.875rem;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      cursor: pointer;
      transition: transform 0.2s, box-shadow 0.2s;
      box-shadow: var(--shadow-md);
    }
    .new-flight-btn:active { transform: scale(0.95); }
    .new-flight-btn span { font-size: 1rem; }
    .nav-items {
      flex: 1;
      padding: 0 0.75rem;
      display: flex;
      flex-direction: column;
      gap: 4px;
    }
    .nav-item {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.75rem 1rem;
      color: #64748b;
      text-decoration: none;
      font-size: 0.875rem;
      font-weight: 500;
      border-radius: 0 8px 8px 0;
      transition: all 0.2s;
      cursor: pointer;
    }
    .nav-item:hover {
      background: #f1f5f9;
      transform: translateX(4px);
    }
    .nav-item.active {
      background: #eff6ff;
      color: #1d4ed8;
      border-right: 4px solid #1d4ed8;
      font-weight: 600;
    }
    .nav-footer {
      margin-top: auto;
      padding: 0 0.75rem;
      border-top: 1px solid rgba(226, 232, 240, 0.5);
      padding-top: 1rem;
      display: flex;
      flex-direction: column;
      gap: 4px;
    }
    .logout { color: var(--error) !important; }
    .logout:hover { background: rgba(186, 26, 26, 0.08) !important; }

    @media (max-width: 768px) {
      .side-nav { display: none; }
    }
  `]
})
export class SideNavbarComponent {
  @Input() subtitle = 'Admin Console';
  @Input() navItems: NavItem[] = [];

  constructor(public authService: AuthService) {}
}
