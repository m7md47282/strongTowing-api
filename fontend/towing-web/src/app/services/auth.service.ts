import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, throwError, of } from 'rxjs';
import { map, catchError, switchMap, shareReplay } from 'rxjs/operators';
import { ApiService } from './api.service';
import { User, LoginRequest, LoginResponse, RegisterRequest, ForgotPasswordRequest, ResetPasswordRequest, OtpVerificationRequest, RefreshTokenResponse } from '../models/user.model';
import { RoleId } from '../constants/user-roles.constants';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();
  private refreshTokenInProgress: Observable<string> | null = null;

  constructor(private apiService: ApiService) {
    // Check if user is already logged in
    const token = localStorage.getItem('stongTowing_token');
    const user = localStorage.getItem('stongTowing_user');
    if (token && user) {
      this.currentUserSubject.next(JSON.parse(user));
    }
    // Check token expiration on initialization
    this.checkAndRefreshTokenIfNeeded();
  }

  // Login
  login(credentials: LoginRequest): Observable<LoginResponse> {
    // Only send email and password to API (not rememberMe)
    const loginPayload = {
      email: credentials.email,
      password: credentials.password
    };

    return this.apiService.post<LoginResponse>('auth/login', loginPayload).pipe(
      map((response: LoginResponse) => {
        if (response.token) {
          localStorage.setItem('stongTowing_token', response.token);
          localStorage.setItem('stongTowing_user', JSON.stringify(response.user));
          if (response.expiresAt) {
            localStorage.setItem('stongTowing_tokenExpiresAt', response.expiresAt);
          }
          // Store refresh token if provided
          if (response.refreshToken) {
            localStorage.setItem('stongTowing_refreshToken', response.refreshToken);
          }
          this.currentUserSubject.next(response.user);
        }
        return response;
      }),
      catchError(error => {
        console.error('Login error:', error);
        return throwError(() => error);
      })
    );
  }

  // Register
  register(userData: RegisterRequest): Observable<any> {
    // Register endpoint is public (no authentication required)
    return this.apiService.post('auth/signup', userData, false).pipe(
      catchError(error => {
        console.error('Registration error:', error);
        return throwError(() => error);
      })
    );
  }

  // Forgot Password
  forgotPassword(request: ForgotPasswordRequest): Observable<any> {
    return this.apiService.post('auth/forgot-password', request).pipe(
      catchError(error => {
        console.error('Forgot password error:', error);
        return throwError(() => error);
      })
    );
  }

  // Reset Password
  resetPassword(request: ResetPasswordRequest): Observable<any> {
    return this.apiService.post('auth/reset-password', request).pipe(
      catchError(error => {
        console.error('Reset password error:', error);
        return throwError(() => error);
      })
    );
  }

  // Verify OTP
  verifyOtp(request: OtpVerificationRequest): Observable<any> {
    return this.apiService.post('auth/verify-otp', request).pipe(
      catchError(error => {
        console.error('OTP verification error:', error);
        return throwError(() => error);
      })
    );
  }

  // Resend OTP
  resendOtp(email: string): Observable<any> {
    return this.apiService.post('auth/resend-otp', { email }).pipe(
      catchError(error => {
        console.error('Resend OTP error:', error);
        return throwError(() => error);
      })
    );
  }

  // Logout
  logout(): Observable<any> {
    return this.apiService.post('auth/logout', {}).pipe(
      map(() => {
        // Clear local storage and user state regardless of API response
        this.clearLocalStorage();
        return { success: true };
      }),
      catchError(error => {
        // Even if API call fails, clear local storage
        this.clearLocalStorage();
        return throwError(() => error);
      })
    );
  }

  // Clear local storage (private helper)
  private clearLocalStorage(): void {
    localStorage.removeItem('stongTowing_token');
    localStorage.removeItem('stongTowing_user');
    localStorage.removeItem('stongTowing_tokenExpiresAt');
    localStorage.removeItem('stongTowing_refreshToken'); // Clear refresh token too
    this.currentUserSubject.next(null);
  }

  // Get current user
  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
  }

  // Check if user is authenticated
  isAuthenticated(): boolean {
    const token = localStorage.getItem('stongTowing_token');
    if (!token) {
      return false;
    }
    
    // Check if token is expired
    if (this.isTokenExpiredOrExpiringSoon()) {
      const refreshToken = this.getRefreshToken();
      // If we have a refresh token, we're still considered authenticated
      // The interceptor will handle the refresh
      return !!refreshToken;
    }
    
    return true;
  }

  // Refresh token method
  refreshToken(): Observable<RefreshTokenResponse> {
    const refreshToken = this.getRefreshToken();
    
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token available'));
    }

    // If a refresh is already in progress, return that observable
    if (this.refreshTokenInProgress) {
      return this.refreshTokenInProgress.pipe(
        switchMap(token => of({ token, expiresAt: this.getTokenExpiration() } as RefreshTokenResponse))
      );
    }

    // Start new refresh token request
    this.refreshTokenInProgress = this.apiService.post<RefreshTokenResponse>(
      'auth/refresh-token', 
      { refreshToken },
      false // Don't include auth header for refresh endpoint
    ).pipe(
      map((response: RefreshTokenResponse) => {
        // Update tokens
        localStorage.setItem('stongTowing_token', response.token);
        if (response.expiresAt) {
          localStorage.setItem('stongTowing_tokenExpiresAt', response.expiresAt);
        }
        if (response.refreshToken) {
          localStorage.setItem('stongTowing_refreshToken', response.refreshToken);
        }
        
        this.refreshTokenInProgress = null;
        return response.token;
      }),
      catchError((error) => {
        this.refreshTokenInProgress = null;
        // Clear tokens on refresh failure
        this.clearLocalStorage();
        return throwError(() => error);
      }),
      shareReplay(1) // Share the result with multiple subscribers
    );

    return this.refreshTokenInProgress.pipe(
      switchMap(token => of({ token, expiresAt: this.getTokenExpiration() } as RefreshTokenResponse))
    );
  }

  // Get refresh token from storage
  getRefreshToken(): string | null {
    return localStorage.getItem('stongTowing_refreshToken');
  }

  // Check if token is expired or expiring soon
  isTokenExpiredOrExpiringSoon(): boolean {
    const expiresAt = localStorage.getItem('stongTowing_tokenExpiresAt');
    if (!expiresAt) {
      return true; // No expiration date means expired
    }

    const expirationTime = new Date(expiresAt).getTime();
    const currentTime = new Date().getTime();
    const fiveMinutesInMs = 5 * 60 * 1000; // 5 minutes buffer

    return (expirationTime - currentTime) <= fiveMinutesInMs;
  }

  // Get token expiration date
  getTokenExpiration(): string {
    return localStorage.getItem('stongTowing_tokenExpiresAt') || '';
  }

  // Check and refresh token if needed (called on app initialization)
  private checkAndRefreshTokenIfNeeded(): void {
    const token = localStorage.getItem('stongTowing_token');
    const refreshToken = this.getRefreshToken();
    
    if (token && refreshToken && this.isTokenExpiredOrExpiringSoon()) {
      // Silently refresh token in background
      this.refreshToken().subscribe({
        next: () => {
          console.log('Token refreshed successfully');
        },
        error: (error) => {
          console.error('Failed to refresh token on init:', error);
          // Don't logout here, let the interceptor handle it
        }
      });
    }
  }

  // Check if user is admin (SuperAdmin or Administrator)
  isAdmin(): boolean {
    const user = this.getCurrentUser();
    return !!(user && (user.roleId === RoleId.SuperAdmin || user.roleId === RoleId.Admin));
  }

  // Check if user is dispatcher
  isDispatcher(): boolean {
    const user = this.getCurrentUser();
    return !!(user && user.roleId === RoleId.Dispatcher);
  }

  // Check if user is driver
  isDriver(): boolean {
    const user = this.getCurrentUser();
    return !!(user && user.roleId === RoleId.Driver);
  }

  // Check if user is super admin
  isSuperAdmin(): boolean {
    const user = this.getCurrentUser();
    return !!(user && user.roleId === RoleId.SuperAdmin);
  }

  // Update user profile
  updateProfile(userData: Partial<User>): Observable<any> {
    return this.apiService.put('auth/profile', userData).pipe(
      map((response: any) => {
        if (response.user) {
          localStorage.setItem('stongTowing_user', JSON.stringify(response.user));
          this.currentUserSubject.next(response.user);
        }
        return response;
      }),
      catchError(error => {
        console.error('Update profile error:', error);
        return throwError(() => error);
      })
    );
  }

  // Change password
  changePassword(currentPassword: string, newPassword: string): Observable<any> {
    return this.apiService.post<any>('auth/change-password', {
      currentPassword,
      newPassword
    }).pipe(
      map((response) => {
        if (response?.id) {
          const stored = this.getCurrentUser();
          const roleId =
            typeof response.roleId === 'string'
              ? parseInt(response.roleId, 10)
              : Number(response.roleId);
          const merged: User = {
            ...(stored ?? ({} as User)),
            id: response.id,
            email: response.email,
            fullName: response.fullName,
            phoneNumber: response.phoneNumber ?? null,
            roleId: Number.isFinite(roleId) ? roleId : (stored?.roleId ?? 0),
            isActive: response.isActive ?? stored?.isActive ?? true,
            createdAt: response.createdAt ?? stored?.createdAt ?? '',
            updatedAt: response.updatedAt ?? null,
            isAvailableForDispatch:
              response.isAvailableForDispatch ?? stored?.isAvailableForDispatch ?? true
          };
          localStorage.setItem('stongTowing_user', JSON.stringify(merged));
          this.currentUserSubject.next(merged);
        }
        return response;
      }),
      catchError(error => {
        console.error('Change password error:', error);
        return throwError(() => error);
      })
    );
  }

  /** Driver: set on-duty / off-duty for new assignments. */
  updateDriverAvailability(isAvailableForDispatch: boolean): Observable<User> {
    return this.apiService.put<User>('auth/driver/availability', { isAvailableForDispatch }).pipe(
      map((response: User) => {
        const stored = this.getCurrentUser();
        const roleId =
          typeof (response as any).roleId === 'string'
            ? parseInt((response as any).roleId, 10)
            : Number((response as any).roleId);
        const merged: User = {
          ...(stored ?? ({} as User)),
          id: response.id,
          email: response.email,
          fullName: response.fullName,
          phoneNumber: response.phoneNumber ?? null,
          roleId: Number.isFinite(roleId) ? roleId : (stored?.roleId ?? 0),
          isActive: response.isActive ?? stored?.isActive ?? true,
          createdAt: response.createdAt ?? stored?.createdAt ?? '',
          updatedAt: response.updatedAt ?? null,
          isAvailableForDispatch: response.isAvailableForDispatch ?? isAvailableForDispatch
        };
        localStorage.setItem('stongTowing_user', JSON.stringify(merged));
        this.currentUserSubject.next(merged);
        return merged;
      }),
      catchError((error) => {
        console.error('Update driver availability error:', error);
        return throwError(() => error);
      })
    );
  }
}