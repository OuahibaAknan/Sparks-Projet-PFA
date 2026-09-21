import { inject } from '@angular/core';
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { environment } from '../../../environments/environment';

const AUTH_ENDPOINTS = [`${environment.apiBaseUrl}/auth/login`, `${environment.apiBaseUrl}/auth/refresh`];

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  if (!req.url.startsWith(environment.apiBaseUrl)) return next(req);

  // Login/refresh calls are unauthenticated by nature — a 401 here means bad credentials or an
  // expired refresh token, not "need to refresh and retry". Letting them through the retry logic
  // below causes an infinite refresh loop when a stale refresh token sits in localStorage.
  const isAuthEndpoint = AUTH_ENDPOINTS.some((url) => req.url.startsWith(url));

  const token = auth.getAccessToken();
  const authedReq = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authedReq).pipe(
    catchError((err: HttpErrorResponse) => {
      if (!isAuthEndpoint && err.status === 401 && auth.getRefreshToken()) {
        return auth.refresh().pipe(
          switchMap(() => {
            const retried = req.clone({ setHeaders: { Authorization: `Bearer ${auth.getAccessToken()}` } });
            return next(retried);
          }),
          catchError((refreshErr) => {
            auth.logout();
            return throwError(() => refreshErr);
          })
        );
      }
      return throwError(() => err);
    })
  );
};
