import { Injectable, signal } from '@angular/core';

export interface Toast {
  id: number;
  message: string;
  type: 'success' | 'error' | 'info';
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  toasts = signal<Toast[]>([]);
  private nextId = 0;

  showSuccess(message: string) {
    this.add(message, 'success');
  }

  showError(message: string) {
    this.add(message, 'error');
  }

  showInfo(message: string) {
    this.add(message, 'info');
  }

  private add(message: string, type: 'success' | 'error' | 'info') {
    const id = this.nextId++;
    this.toasts.update(current => [...current, { id, message, type }]);

    // Auto remove after 5 seconds
    setTimeout(() => this.remove(id), 5000);
  }

  remove(id: number) {
    this.toasts.update(current => current.filter(t => t.id !== id));
  }
}
