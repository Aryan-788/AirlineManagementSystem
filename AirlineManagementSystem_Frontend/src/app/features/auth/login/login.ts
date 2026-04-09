import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { AgentService } from '../../../core/services/agent.service';
import { Clerk } from '@clerk/clerk-js';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class LoginComponent implements OnInit {
  activeTab = signal<'login' | 'register' | 'register_verify' | 'forgot_email' | 'forgot_reset'>('login');
  role = signal('Passenger');
  email = signal('');
  password = signal('');
  name = signal('');
  loading = signal(false);
  error = signal('');
  success = signal('');

  // Password reset specific
  resetToken = signal('');
  newPassword = signal('');
  confirmPassword = signal('');

  // Registration verification specific
  verifyToken = signal('');

  // Maps role strings to their backend enum integer values
  private roleMap: Record<string, number> = {
    'Admin': 0,
    'Passenger': 1,
    'Dealer': 2,
    'GroundStaff': 3
  };

  private clerk: Clerk | null = null;

  private returnUrl = '';

  constructor(
    private authService: AuthService, 
    private agentService: AgentService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.route.queryParams.subscribe(params => {
      this.returnUrl = params['returnUrl'] || '';
    });
    
    if (this.authService.isLoggedIn()) {
      this.redirectAfterLogin();
    }
  }

  private redirectAfterLogin() {
    if (this.authService.userRole() === 'Dealer') {
      this.agentService.createDealer({
        dealerName: this.authService.userName() || 'Dealer',
        dealerEmail: this.authService.userEmail() || this.email(),
        allocatedSeats: 0,
        commissionRate: 5
      }).subscribe({
        next: () => {
          this.performRedirect();
        },
        error: (err) => {
          console.error('Failed to initialize Agent Service Dealer record:', err);
          this.performRedirect(); // Still redirect since auth succeeded
        }
      });
    } else {
      this.performRedirect();
    }
  }

  private performRedirect() {
    if (this.returnUrl) {
      this.router.navigateByUrl(this.returnUrl);
    } else {
      this.router.navigate([this.authService.getDefaultRoute()]);
    }
  }

  async ngOnInit() {
    try {
      this.clerk = new Clerk(environment.clerkPublishableKey);
      
      // Restore URL if Angular router stripped Clerk's OAuth tokens
      const rawUrl = window.sessionStorage.getItem('clerk_raw_url');
      if (rawUrl) {
        window.history.replaceState(null, '', rawUrl);
        window.sessionStorage.removeItem('clerk_raw_url');
      }

      await this.clerk.load();
      
      try {
        if (window.location.href.includes('__clerk') || window.location.hash.includes('clerk')) {
          await this.clerk.handleRedirectCallback();
        }
      } catch (err) {
        console.warn('Clerk handled callback automatically or failed:', err);
      }

      console.log('Clerk loaded. User state:', this.clerk.user ? 'Logged In' : 'Logged Out');
      
      // If logged out but redirect params are detected, perform a final wait for session hydration
      if (!this.clerk.user && (window.location.search.includes('clerk') || window.location.hash.includes('clerk') || window.sessionStorage.getItem('clerk_raw_url'))) {
        console.log('Syncing Clerk session... Waiting 1500ms for hydration.');
        await new Promise(resolve => setTimeout(resolve, 1500));
        console.log('Post-wait User state:', this.clerk.user ? 'Logged In' : 'Logged Out');
      }

      if (this.clerk.user) {
        const email = this.clerk.user.primaryEmailAddress?.emailAddress || '';
        const name = this.clerk.user.fullName || 'Passenger User';
        
        if (email) {
          this.loading.set(true);
          this.authService.oauthLogin({ email, name }).subscribe({
            next: () => {
              this.loading.set(false);
              this.redirectAfterLogin();
            },
            error: (err) => {
              this.loading.set(false);
              console.error('OAuth backend login failed', err);
              this.error.set('Failed to sync authentication with backend.');
              this.clerk?.signOut();
            }
          });
        } else {
          console.error("Clerk user exists but has no primary email");
        }
      }
      
      // Fallback listener in case session hydrates asynchronously
      this.clerk.addListener(({ user }) => {
        if (user && !this.authService.isLoggedIn() && !this.loading()) {
          const email = user.primaryEmailAddress?.emailAddress;
          if (email) {
            this.loading.set(true);
            this.authService.oauthLogin({ email, name: user.fullName || 'Passenger User' }).subscribe({
              next: () => {
                this.loading.set(false);
                this.redirectAfterLogin();
              },
              error: () => {
                this.loading.set(false);
                this.clerk?.signOut();
              }
            });
          }
        }
      });
      
    } catch (e) {
      console.error('Failed to initialize Clerk:', e);
    }
  }

  switchTab(tab: 'login' | 'register' | 'register_verify' | 'forgot_email' | 'forgot_reset') {
    this.activeTab.set(tab);
    this.error.set('');
    this.success.set('');
  }

  requestPasswordReset() {
    if (!this.email()) {
      this.error.set('Please enter your email address.');
      return;
    }
    this.loading.set(true);
    this.error.set('');
    this.success.set('');
    this.authService.forgotPassword(this.email()).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set('If the email is registered, a reset link with OTP has been sent. Check your inbox.');
        this.activeTab.set('forgot_reset');
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.message || 'Failed to request password reset.');
      }
    });
  }

  confirmPasswordReset() {
    if (!this.resetToken() || !this.newPassword() || !this.confirmPassword()) {
      this.error.set('Please fill out all fields.');
      return;
    }
    if (this.newPassword() !== this.confirmPassword()) {
      this.error.set('Passwords do not match.');
      return;
    }

    this.loading.set(true);
    this.error.set('');
    this.authService.resetPassword({
      email: this.email(),
      token: this.resetToken(),
      newPassword: this.newPassword()
    }).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set('Password has been reset successfully. You can now login.');
        this.activeTab.set('login');
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.message || 'Failed to reset password. Token might be invalid or expired.');
      }
    });
  }

  confirmRegistration() {
    if (!this.verifyToken() || !this.email()) {
      this.error.set('Please enter the OTP sent to your email.');
      return;
    }

    this.loading.set(true);
    this.error.set('');
    this.success.set('');
    this.authService.verifyRegistration({ email: this.email(), token: this.verifyToken() }).subscribe({
      next: () => {
        this.loading.set(false);
        this.redirectAfterLogin();
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.message || 'Verification failed. Please check your OTP.');
      }
    });
  }

  resendVerification() {
    if (!this.email()) {
      this.error.set('Please enter your email first.');
      return;
    }
    
    this.loading.set(true);
    this.error.set('');
    this.success.set('');
    
    this.authService.resendVerification(this.email()).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set('A new verification OTP has been sent. Check your inbox.');
        this.activeTab.set('register_verify');
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.message || 'Failed to resend OTP.');
      }
    });
  }

  onSubmit() {
    this.loading.set(true);
    this.error.set('');
    this.success.set('');

    if (this.activeTab() === 'login') {
      this.authService.login({ email: this.email(), password: this.password() }).subscribe({
        next: () => {
          this.loading.set(false);
          this.redirectAfterLogin();
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(err.error?.message || 'Login failed. Please check your credentials.');
        }
      });
    } else if (this.activeTab() === 'register') {
      this.authService.register({
        name: this.name(),
        email: this.email(),
        password: this.password(),
        role: this.roleMap[this.role()] ?? 1
      }).subscribe({
        next: (res) => {
          this.loading.set(false);
          this.success.set(res.message || 'Registration successful. Please check your email for the OTP.');
          this.activeTab.set('register_verify');
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(err.error?.message || 'Registration failed. Please try again.');
        }
      });
    }
  }

  async loginWithOAuth(strategy: string) {
    if (!this.clerk) {
      this.error.set('Authentication service is currently unavailable.');
      return;
    }

    try {
      await this.clerk.client!.signIn!.authenticateWithRedirect({
        strategy: strategy as any,
        redirectUrl: `${window.location.origin}/login`,
        redirectUrlComplete: `${window.location.origin}/login`
      });
    } catch (e) {
      console.error('Clerk OAuth Error:', e);
      try {
        this.clerk.openSignIn();
      } catch (fallbackError) {
        console.error('Fallback openSignIn failed:', fallbackError);
        this.error.set('Failed to initialize OAuth. Please try again.');
      }
    }
  }
}
