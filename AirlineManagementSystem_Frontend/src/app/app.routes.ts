import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'passenger/search', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login').then(m => m.LoginComponent)
  },
  // Passenger Routes
  {
    path: 'passenger/dashboard',
    loadComponent: () => import('./features/passenger/dashboard/dashboard').then(m => m.PassengerDashboardComponent),
    canActivate: [authGuard, roleGuard(['Passenger'])]
  },
  {
    path: 'passenger/search',
    loadComponent: () => import('./features/passenger/search/search').then(m => m.FlightSearchComponent)
  },
  { path: 'passenger/booking/:id', loadComponent: () => import('./features/passenger/booking/booking').then(m => m.BookingComponent), canActivate: [authGuard, roleGuard(['Passenger', 'Dealer'])] },
  { path: 'passenger/booking/schedule/:scheduleId', loadComponent: () => import('./features/passenger/booking/booking').then(m => m.BookingComponent), canActivate: [authGuard, roleGuard(['Passenger', 'Dealer'])] },
  { path: 'passenger/payment/:id', loadComponent: () => import('./features/passenger/payment/payment').then(m => m.PaymentComponent), canActivate: [authGuard, roleGuard(['Passenger', 'Dealer'])] },
  {
    path: 'passenger/my-bookings',
    loadComponent: () => import('./features/passenger/my-bookings/my-bookings').then(m => m.MyBookingsComponent),
    canActivate: [authGuard, roleGuard(['Passenger', 'Dealer'])]
  },
  {
    path: 'passenger/rewards',
    loadComponent: () => import('./features/passenger/rewards/rewards').then(m => m.RewardsComponent),
    canActivate: [authGuard, roleGuard(['Passenger'])]
  },
  {
    path: 'passenger/check-in',
    loadComponent: () => import('./features/passenger/check-in/check-in').then(m => m.CheckInComponent),
    canActivate: [authGuard, roleGuard(['Passenger'])]
  },
  // Admin Routes
  {
    path: 'admin/dashboard',
    loadComponent: () => import('./features/admin/dashboard/dashboard').then(m => m.AdminDashboardComponent),
    canActivate: [authGuard, roleGuard(['Admin'])]
  },
  {
    path: 'admin/flights',
    loadComponent: () => import('./features/admin/flights/flights').then(m => m.AdminFlightsComponent),
    canActivate: [authGuard, roleGuard(['Admin'])]
  },
  {
    path: 'admin/schedules',
    loadComponent: () => import('./features/admin/schedules/schedules').then(m => m.AdminSchedulesComponent),
    canActivate: [authGuard, roleGuard(['Admin'])]
  },
  {
    path: 'admin/refunds',
    loadComponent: () => import('./features/admin/refunds/refunds').then(m => m.AdminRefundsComponent),
    canActivate: [authGuard, roleGuard(['Admin'])]
  },
  // Dealer Routes
  {
    path: 'dealer/dashboard',
    loadComponent: () => import('./features/dealer/dashboard/dashboard').then(m => m.DealerDashboardComponent),
    canActivate: [authGuard, roleGuard(['Dealer'])]
  },
  {
    path: 'dealer/search',
    loadComponent: () => import('./features/passenger/search/search').then(m => m.FlightSearchComponent),
    canActivate: [authGuard, roleGuard(['Dealer'])]
  },
  {
    path: 'dealer/booking/:id',
    loadComponent: () => import('./features/passenger/booking/booking').then(m => m.BookingComponent),
    canActivate: [authGuard, roleGuard(['Dealer'])]
  },
  {
    path: 'dealer/booking/schedule/:scheduleId',
    loadComponent: () => import('./features/passenger/booking/booking').then(m => m.BookingComponent),
    canActivate: [authGuard, roleGuard(['Dealer'])]
  },
  {
    path: 'dealer/payment/:id',
    loadComponent: () => import('./features/passenger/payment/payment').then(m => m.PaymentComponent),
    canActivate: [authGuard, roleGuard(['Dealer'])]
  },
  {
    path: 'dealer/my-bookings',
    loadComponent: () => import('./features/passenger/my-bookings/my-bookings').then(m => m.MyBookingsComponent),
    canActivate: [authGuard, roleGuard(['Dealer'])]
  },
  // Ground Staff Routes
  {
    path: 'ground-staff/baggage',
    loadComponent: () => import('./features/ground-staff/baggage/baggage').then(m => m.GroundStaffBaggageComponent),
    canActivate: [authGuard, roleGuard(['GroundStaff'])]
  },
  // Wildcard redirect
  { path: '**', redirectTo: 'passenger/search' }
];
