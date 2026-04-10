import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Skip token refresh for auth endpoints and public endpoints
  if (req.url.includes('/auth/login') || 
      req.url.includes('/auth/register') || 
      req.url.includes('/auth/signup') ||
      req.url.includes('/auth/forgot-password') ||
      req.url.includes('/auth/reset-password') ||
      req.url.includes('/auth/verify-otp') ||
      req.url.includes('/auth/resend-otp') ||
      req.url.includes('/auth/refresh-token') ||
      req.url.includes('/health') ||
      req.url.includes('/location/')) {
    return next(req);
  }

  // Check if token is expired or about to expire (within 5 minutes)
  if (authService.isTokenExpiredOrExpiringSoon()) {
    const refreshToken = authService.getRefreshToken();
    
    if (refreshToken) {
      // Attempt to refresh token before making the request
      return authService.refreshToken().pipe(
        switchMap((response) => {
          // Retry the original request with new token
          const clonedReq = req.clone({
            setHeaders: {
              Authorization: `Bearer ${response.token}`
            }
          });
          return next(clonedReq);
        }),
        catchError((error) => {
          // If refresh fails, logout and redirect to login
          if (error.status === 401 || error.status === 403) {
            authService.logout().subscribe(() => {
              router.navigate(['/login'], { 
                queryParams: { returnUrl: router.url } 
              });
            });
          }
          return throwError(() => error);
        })
      );
    }
  }

  // If no refresh token or token is still valid, proceed normally
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Handle 401 Unauthorized errors
      if (error.status === 401 && !req.url.includes('/auth/')) {
        const refreshToken = authService.getRefreshToken();
        
        if (refreshToken) {
          // Try to refresh token and retry the request
          return authService.refreshToken().pipe(
            switchMap((response) => {
              // Retry the original request with new token
              const clonedReq = req.clone({
                setHeaders: {
                  Authorization: `Bearer ${response.token}`
                }
              });
              return next(clonedReq);
            }),
            catchError((refreshError) => {
              // If refresh fails, logout and redirect
              authService.logout().subscribe(() => {
                router.navigate(['/login'], { 
                  queryParams: { returnUrl: router.url } 
                });
              });
              return throwError(() => refreshError);
            })
          );
        } else {
          // No refresh token, redirect to login
          authService.logout().subscribe(() => {
            router.navigate(['/login'], { 
              queryParams: { returnUrl: router.url } 
            });
          });
        }
      }
      
      return throwError(() => error);
    })
  );
};
