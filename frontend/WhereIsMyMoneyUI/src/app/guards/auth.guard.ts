import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { CookieService } from 'ngx-cookie-service';

export const authGuard: CanActivateFn = () => {
  const router = inject(Router);
  const cookieService = inject(CookieService);
  const token = cookieService.get('authToken');

  if (token) {
    return true;
  }

  return router.createUrlTree(['/account/login']);
};

export const guestOnlyGuard: CanActivateFn = () => {
  const router = inject(Router);
  const cookieService = inject(CookieService);
  const token = cookieService.get('authToken');

  if (!token) {
    return true;
  }

  return router.createUrlTree(['/dashboard']);
};
