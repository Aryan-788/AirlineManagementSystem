import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { UpdateProfileRequest } from '../../core/models/api.models';

@Component({
  selector: 'app-profile-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="modal-backdrop" *ngIf="isOpen" (click)="close.emit()">
      <div class="modal-container shadow-2xl" (click)="$event.stopPropagation()">
        <div class="modal-header">
          <h2 class="font-headline">Update Profile</h2>
          <button class="icon-btn" (click)="close.emit()">
            <span class="material-symbols-outlined">close</span>
          </button>
        </div>
        
        <div class="modal-body">
          <div class="alert success" *ngIf="successMessage()">{{ successMessage() }}</div>
          <div class="alert error" *ngIf="errorMessage()">{{ errorMessage() }}</div>
          
          <div class="form-group">
            <label>Name</label>
            <input type="text" [(ngModel)]="profileData.name" class="form-input" placeholder="Your Name" />
          </div>
          
          <div class="form-group">
            <label>Email</label>
            <input type="email" [(ngModel)]="profileData.email" class="form-input" placeholder="Your Email" />
          </div>
        </div>
        
        <div class="modal-footer">
          <button class="btn btn-outline" (click)="close.emit()">Cancel</button>
          <button class="btn btn-primary" (click)="saveProfile()" [disabled]="isLoading()">
            {{ isLoading() ? 'Saving...' : 'Save Changes' }}
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .modal-backdrop {
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background: rgba(15, 23, 42, 0.4);
      backdrop-filter: blur(4px);
      z-index: 1000;
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .modal-container {
      background: white;
      border-radius: 16px;
      width: 100%;
      max-width: 480px;
      overflow: hidden;
      animation: modalFadeIn 0.3s cubic-bezier(0.16, 1, 0.3, 1);
    }
    .modal-header {
      padding: 1.5rem;
      border-bottom: 1px solid var(--surface-container);
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .modal-header h2 { margin: 0; font-size: 1.25rem; font-weight: 700; color: #0f172a; }
    .icon-btn { background: none; border: none; cursor: pointer; color: #64748b; padding: 4px; border-radius: 50%; transition: background 0.2s; }
    .icon-btn:hover { background: #f1f5f9; color: #0f172a; }
    .modal-body { padding: 1.5rem; display: flex; flex-direction: column; gap: 1.25rem; }
    .form-group { display: flex; flex-direction: column; gap: 0.5rem; }
    .form-group label { font-size: 0.875rem; font-weight: 600; color: #475569; }
    .form-input { padding: 0.75rem 1rem; border: 1px solid var(--outline-variant); border-radius: 8px; font-size: 0.875rem; background: var(--surface-container-low); transition: border-color 0.2s; }
    .form-input:focus { outline: none; border-color: var(--primary); background: white; }
    .modal-footer { padding: 1.25rem 1.5rem; border-top: 1px solid var(--surface-container); display: flex; justify-content: flex-end; gap: 1rem; background: #f8fafc; }
    .btn { padding: 0.625rem 1.25rem; border-radius: 8px; font-size: 0.875rem; font-weight: 600; cursor: pointer; transition: all 0.2s; border: none; }
    .btn-outline { background: white; color: #475569; border: 1px solid var(--outline-variant); }
    .btn-outline:hover { background: #f1f5f9; color: #0f172a; }
    .btn-primary { background: linear-gradient(135deg, var(--primary), var(--primary-container)); color: white; }
    .btn-primary:hover { opacity: 0.9; transform: translateY(-1px); box-shadow: 0 4px 12px rgba(26, 35, 126, 0.2); }
    .btn-primary:disabled { opacity: 0.6; cursor: not-allowed; transform: none; box-shadow: none; }
    .alert { padding: 0.75rem; border-radius: 8px; font-size: 0.875rem; font-weight: 500; }
    .success { background: #f0fdf4; color: #16a34a; border: 1px solid #bbf7d0; }
    .error { background: #fef2f2; color: #ef4444; border: 1px solid #fecaca; }
    @keyframes modalFadeIn {
      from { opacity: 0; transform: translateY(20px) scale(0.95); }
      to { opacity: 1; transform: translateY(0) scale(1); }
    }
  `]
})
export class ProfileModalComponent {
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();
  @Output() profileUpdated = new EventEmitter<void>();

  profileData: UpdateProfileRequest = { name: '', email: '' };
  isLoading = signal(false);
  successMessage = signal('');
  errorMessage = signal('');

  constructor(private authService: AuthService) {}

  ngOnInit() {
    this.loadProfile();
  }

  ngOnChanges() {
    if (this.isOpen) {
      this.loadProfile();
      this.successMessage.set('');
      this.errorMessage.set('');
    }
  }

  loadProfile() {
    const userId = this.authService.userId();
    if (userId) {
      this.authService.getUserProfile(userId).subscribe({
        next: (user) => {
          this.profileData = { name: user.name, email: user.email };
        }
      });
    }
  }

  saveProfile() {
    const userId = this.authService.userId();
    if (!userId) return;

    this.isLoading.set(true);
    this.successMessage.set('');
    this.errorMessage.set('');

    this.authService.updateProfile(userId, this.profileData).subscribe({
      next: (user) => {
        this.isLoading.set(false);
        this.successMessage.set('Profile updated successfully!');
        
        // Update local auth state with new name/email
        const currentDataStr = localStorage.getItem('sky_user');
        if (currentDataStr) {
          const currentData = JSON.parse(currentDataStr);
          currentData.name = user.name;
          currentData.email = user.email;
          // Ideally AuthService should have an update method, but we can call login or re-hydrate
          localStorage.setItem('sky_user', JSON.stringify(currentData));
          setTimeout(() => window.location.reload(), 1500); // Reload to reflect changes
        }
        
        this.profileUpdated.emit();
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to update profile');
      }
    });
  }
}
