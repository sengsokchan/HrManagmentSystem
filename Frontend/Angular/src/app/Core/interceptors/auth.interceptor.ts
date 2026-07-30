import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { HrStateService } from '../services/hr-state.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const state = inject(HrStateService);
  const router = inject(Router);
  const isPublicAuth =
    req.url.includes('/api/auth/login') || req.url.includes('/api/auth/forgot-password');

  let headers = req.headers;
  if (state.token && req.url.startsWith('/api') && !isPublicAuth) {
    headers = headers.set('Authorization', `Bearer ${state.token}`);
  }

  return next(req.clone({ headers })).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !isPublicAuth && state.isAuthenticated) {
        state.handleUnauthorized();
        void router.navigateByUrl('/login');
      }
      return throwError(() => error);
    })
  );
};
