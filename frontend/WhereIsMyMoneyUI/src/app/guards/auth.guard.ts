import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const authGuard: CanActivateFn = async () => {
  const router = inject(Router);
  const token = await cookieStore.get('authToken');

  if (token) {
    return true;
  }

  return router.createUrlTree(['/account/login']);
};

export const guestOnlyGuard: CanActivateFn = async () => {
  const router = inject(Router);
  const token = await cookieStore.get('authToken');

  if (!token) {
    return true;
  }

  return router.createUrlTree(['/dashboard']);
};
