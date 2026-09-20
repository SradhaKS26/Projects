import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../authentication/auth.service';

const ANONYMOUS_AUTH = /\/auth\/(login|register|refresh)(?:\?|$)/;

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const isAnonymousAuth = ANONYMOUS_AUTH.test(req.url);
  const token = auth.getAccessToken();

  const authedReq =
    token && !isAnonymousAuth
      ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : req;

  return next(authedReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || isAnonymousAuth) {
        return throwError(() => error);
      }

      return auth.refreshSession().pipe(
        switchMap(() => {
          const refreshed = auth.getAccessToken();
          if (!refreshed) {
            auth.logout();
            return throwError(() => error);
          }

          return next(
            req.clone({
              setHeaders: { Authorization: `Bearer ${refreshed}` },
            }),
          );
        }),
        catchError((refreshError) => {
          auth.logout();
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};
