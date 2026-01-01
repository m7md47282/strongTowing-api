import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { RoleId } from '../constants/user-roles.constants';

export const roleGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  
  const user = authService.getCurrentUser();
  
  if (!user) {
    router.navigate(['/login']);
    return false;
  }


  const allowedRoles = route.data?.['roles'] as number[];
  const roleId = Number(user.roleId);
  
  if (!allowedRoles || allowedRoles.length === 0) {
    return true; // No role restriction
  }

  if (allowedRoles.includes(roleId)) {
    return true;
  }

  // Redirect based on user role
  if (roleId  === RoleId.SuperAdmin || roleId === RoleId.Admin) {
    router.navigate(['/admin']);
  } else if (roleId === RoleId.Dispatcher) {
    router.navigate(['/admin']);
  } else if (roleId === RoleId.Driver) {
    router.navigate(['/driver']);
  } else {
    router.navigate(['/dashboard']);
  }
  
  return false;
};

