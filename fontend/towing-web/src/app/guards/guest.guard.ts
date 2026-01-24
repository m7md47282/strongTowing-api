import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { RoleId } from '../constants/user-roles.constants';

export const guestGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  
  // If user is not authenticated, allow access to landing page
  if (!authService.isAuthenticated()) {
    return true;
  }
  
  // If authenticated, redirect to appropriate dashboard
  const user = authService.getCurrentUser();
  if (!user) {
    router.navigate(['/customer']);
    return false;
  }

  // Ensure roleId is a number for comparison (handle both string and number)
  const roleId = typeof user.roleId === 'string' ? parseInt(user.roleId, 10) : Number(user.roleId);
  
  if (roleId === RoleId.SuperAdmin || roleId === RoleId.Admin) {
    router.navigate(['/admin']);
  } else if (roleId === RoleId.Dispatcher) {
    router.navigate(['/dispatcher']);
  } else if (roleId === RoleId.Driver) {
    router.navigate(['/driver']);
  } else if (roleId === RoleId.User) {
    router.navigate(['/customer']);
  } else {
    // Default to customer dashboard
    router.navigate(['/customer']);
  }
  
  return false;
};
