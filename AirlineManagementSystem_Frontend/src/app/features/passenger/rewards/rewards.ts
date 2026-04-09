import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TopNavbarComponent } from '../../../shared/components/top-navbar.component';
import { SideNavbarComponent } from '../../../shared/components/side-navbar.component';
import { RewardService } from '../../../core/services/reward.service';
import { AuthService } from '../../../core/services/auth.service';
import { RewardBalance } from '../../../core/models/api.models';

@Component({
  selector: 'app-rewards',
  standalone: true,
  imports: [CommonModule, RouterModule, TopNavbarComponent, SideNavbarComponent],
  templateUrl: './rewards.html',
  styleUrl: './rewards.css'
})
export class RewardsComponent implements OnInit {
  rewardBalance = signal<RewardBalance>({ userId: 0, totalPoints: 0 });
  loading = signal(true);

  sideNavItems = [
    { icon: 'dashboard', label: 'Dashboard', route: '/passenger/dashboard' },
    { icon: 'travel_explore', label: 'Search Flights', route: '/passenger/search' },
    { icon: 'confirmation_number', label: 'My Bookings', route: '/passenger/my-bookings' },
    { icon: 'stars', label: 'Rewards', route: '/passenger/rewards' },
  ];

  perks = [
    { icon: 'luggage', title: 'Extra Baggage', desc: 'Add 15kg checked baggage to your booking', cost: 2000, badge: 'Popular' },
    { icon: 'lounge', title: 'Lounge Access', desc: 'Complimentary access to premium airport lounges', cost: 5000, badge: 'Elite' },
    { icon: 'airline_seat_recline_extra', title: 'Seat Upgrade', desc: 'Upgrade to Business Class from Economy on your next flight', cost: 8500, badge: 'Premium' },
    { icon: 'card_travel', title: 'Priority Boarding', desc: 'Board first and settle in before other passengers', cost: 1200, badge: '' },
    { icon: 'restaurant', title: 'Inflight Meals', desc: 'Special curated meals delivered to your seat', cost: 800, badge: 'New' },
    { icon: 'wifi', title: 'Inflight Wi-Fi', desc: 'Stay connected throughout your entire journey', cost: 1500, badge: '' },
  ];

  constructor(
    public authService: AuthService,
    private rewardService: RewardService
  ) {}

  ngOnInit() {
    const userId = this.authService.userId();
    if (userId) {
      this.rewardService.getBalance(userId).subscribe({
        next: (data) => { this.rewardBalance.set(data); this.loading.set(false); },
        error: () => this.loading.set(false)
      });
    }
  }

  get progressPercent(): number {
    const pts = this.rewardBalance().totalPoints;
    const platinum = 140000;
    return Math.min(Math.round((pts / platinum) * 100), 100);
  }

  get memberTier(): string {
    const pts = this.rewardBalance().totalPoints;
    if (pts >= 100000) return 'Platinum';
    if (pts >= 50000) return 'Gold';
    if (pts >= 20000) return 'Silver';
    return 'Bronze';
  }

  get nextTier(): string {
    const tier = this.memberTier;
    if (tier === 'Bronze') return 'Silver';
    if (tier === 'Silver') return 'Gold';
    if (tier === 'Gold') return 'Platinum';
    return 'Platinum';
  }

  get ptsToNextTier(): number {
    const pts = this.rewardBalance().totalPoints;
    const thresholds: { [key: string]: number } = { Bronze: 20000, Silver: 50000, Gold: 100000, Platinum: 140000 };
    const next = thresholds[this.nextTier] || 140000;
    return Math.max(0, next - pts);
  }
}
