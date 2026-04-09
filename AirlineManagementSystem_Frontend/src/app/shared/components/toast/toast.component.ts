import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="toast-container">
      <div *ngFor="let toast of toastService.toasts()" 
           class="toast-item" 
           [class]="toast.type"
           (click)="toastService.remove(toast.id)">
        <span class="material-symbols-outlined icon">
          {{ toast.type === 'success' ? 'check_circle' : toast.type === 'error' ? 'error' : 'info' }}
        </span>
        <span class="message">{{ toast.message }}</span>
        <button class="close-btn">&times;</button>
      </div>
    </div>
  `,
  styles: [`
    .toast-container {
      position: fixed;
      top: 24px;
      right: 24px;
      z-index: 9999;
      display: flex;
      flex-direction: column;
      gap: 12px;
      pointer-events: none;
    }

    .toast-item {
      pointer-events: auto;
      min-width: 300px;
      max-width: 450px;
      padding: 16px;
      border-radius: 12px;
      background: white;
      box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05);
      display: flex;
      align-items: center;
      gap: 12px;
      cursor: pointer;
      animation: slideIn 0.3s ease-out;
      border-left: 6px solid #cbd5e1;
      transition: transform 0.2s;
    }

    .toast-item:hover {
      transform: translateY(-2px);
    }

    .toast-item.success { border-left-color: #22c55e; }
    .toast-item.error { border-left-color: #ef4444; }
    .toast-item.info { border-left-color: #3b82f6; }

    .icon {
      font-size: 24px;
    }

    .success .icon { color: #22c55e; }
    .error .icon { color: #ef4444; }
    .info .icon { color: #3b82f6; }

    .message {
      flex: 1;
      font-size: 14px;
      font-weight: 600;
      color: #1e293b;
      line-height: 1.4;
    }

    .close-btn {
      background: none;
      border: none;
      font-size: 20px;
      color: #94a3b8;
      cursor: pointer;
      padding: 4px;
    }

    @keyframes slideIn {
      from {
        transform: translateX(100%);
        opacity: 0;
      }
      to {
        transform: translateX(0);
        opacity: 1;
      }
    }
  `]
})
export class ToastComponent {
  toastService = inject(ToastService);
}
