import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  
  // If Clerk OAuth is returning directly to a protected route
  if (state.url.includes('clerk=') || state.url.includes('__clerk')) {
    window.sessionStorage.setItem('clerk_raw_url', window.location.href);
  }
  
  if (authService.isLoggedIn()) return true;
  
  router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
  return false;
};

export const roleGuard = (allowedRoles: string[]): CanActivateFn => {
  return (route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);
    if (!authService.isLoggedIn()) {
      router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
      return false;
    }
    if (allowedRoles.includes(authService.userRole())) return true;
    router.navigate([authService.getDefaultRoute()]);
    return false;
  };
};
