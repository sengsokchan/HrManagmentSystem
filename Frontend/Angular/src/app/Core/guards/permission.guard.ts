import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { ViewName } from '../models/hr.models';
import { HrStateService } from '../services/hr-state.service';

export const permissionGuard: CanActivateFn = (route) => {
  const state = inject(HrStateService);
  const router = inject(Router);
  const view = route.routeConfig?.path as ViewName | undefined;

  if (!view || state.canAccessView(view)) {
    return true;
  }

  return router.createUrlTree(['/dashboard']);
};
