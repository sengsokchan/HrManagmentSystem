import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { HrStateService } from '../services/hr-state.service';

export const authGuard: CanActivateFn = () => {
  const state = inject(HrStateService);
  const router = inject(Router);

  if (!state.isAuthenticated) {
    return router.createUrlTree(['/login']);
  }

  if (state.mustChangePassword) {
    return router.createUrlTree(['/change-password']);
  }

  return true;
};

export const guestGuard: CanActivateFn = () => {
  const state = inject(HrStateService);
  const router = inject(Router);

  if (!state.isAuthenticated) {
    return true;
  }

  if (state.mustChangePassword) {
    return router.createUrlTree(['/change-password']);
  }

  return router.createUrlTree(['/dashboard']);
};

/** Allows signed-in users who still need to change their temporary password. */
export const changePasswordGuard: CanActivateFn = () => {
  const state = inject(HrStateService);
  const router = inject(Router);

  if (!state.isAuthenticated) {
    return router.createUrlTree(['/login']);
  }

  if (!state.mustChangePassword) {
    return router.createUrlTree(['/dashboard']);
  }

  return true;
};
