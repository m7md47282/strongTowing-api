import { Routes } from '@angular/router';
import { HomeComponent } from './components/home/home.component';
import { LoginComponent } from './components/auth/login/login.component';
import { RegisterComponent } from './components/auth/register/register.component';
import { ForgotPasswordComponent } from './components/auth/forgot-password/forgot-password.component';
import { AdminComponent } from './components/dashboard/admin/admin.component';
import { DashboardComponent } from './components/dashboard/admin/dashboard/dashboard.component';
import { UsersComponent } from './components/dashboard/admin/users/users.component';
import { DispatcherComponent } from './components/dashboard/dispatcher/dispatcher.component';
import { JobsComponent } from './components/dashboard/dispatcher/jobs/jobs.component';
import { VehiclesComponent } from './components/dashboard/dispatcher/vehicles/vehicles.component';
import { PaymentsComponent } from './components/dashboard/dispatcher/payments/payments.component';
import { DriverComponent } from './components/dashboard/driver/driver.component';
import { CustomerComponent } from './components/dashboard/customer/customer.component';
import { ServicesComponent } from './components/services/services.component';
import { AboutComponent } from './components/about/about.component';
import { RequestServiceComponent } from './components/request-service/request-service.component';
import { roleGuard } from './guards/role.guard';
import { authGuard } from './guards/auth.guard';
import { guestGuard } from './guards/guest.guard';
import { RoleId } from './constants/user-roles.constants';
import { DriverAssignmentsComponent } from './components/dashboard/shared/driver-assignments/driver-assignments.component';
import { SettingsComponent } from './components/dashboard/admin/settings/settings.component';

export const routes: Routes = [
  { 
    path: '', 
    component: HomeComponent,
    canActivate: [guestGuard]
  },
  { 
    path: 'home', 
    component: HomeComponent,
    canActivate: [guestGuard]
  },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'forgot-password', component: ForgotPasswordComponent },
  { 
    path: 'admin', 
    component: AdminComponent,
    canActivate: [roleGuard, authGuard],
    data: { roles: [RoleId.SuperAdmin, RoleId.Admin] },
    children: [
      { path: '', component: DashboardComponent },
      { 
        path: 'users', 
        component: UsersComponent,
        canActivate: [roleGuard, authGuard],
        data: { roles: [RoleId.SuperAdmin, RoleId.Admin] }
      },
      { 
        path: 'drivers',
        component: DriverAssignmentsComponent,
        canActivate: [roleGuard, authGuard],
        data: { roles: [RoleId.SuperAdmin, RoleId.Admin] }
      },
      { 
        path: 'jobs',
        component: JobsComponent,
        canActivate: [roleGuard, authGuard],
        data: { roles: [RoleId.SuperAdmin, RoleId.Admin] }
      },
      { 
        path: 'vehicles',
        component: VehiclesComponent,
        canActivate: [roleGuard, authGuard],
        data: { roles: [RoleId.SuperAdmin, RoleId.Admin] }
      },
      { 
        path: 'payments',
        component: PaymentsComponent,
        canActivate: [roleGuard, authGuard],
        data: { roles: [RoleId.SuperAdmin, RoleId.Admin] }
      },
      {
        path: 'settings',
        component: SettingsComponent,
        canActivate: [roleGuard, authGuard],
        data: { roles: [RoleId.SuperAdmin, RoleId.Admin] }
      }
    ]
  },
  { 
    path: 'dispatcher', 
    component: DispatcherComponent,
    canActivate: [roleGuard, authGuard],
    data: { roles: [RoleId.Dispatcher, RoleId.SuperAdmin, RoleId.Admin] },
    children: [
      { path: '', component: DashboardComponent },
      { path: 'jobs', component: JobsComponent },
      { path: 'drivers', component: DriverAssignmentsComponent },
      { path: 'vehicles', component: VehiclesComponent },
      { path: 'payments', component: PaymentsComponent }
    ]
  },
  { 
    path: 'driver', 
    component: DriverComponent,
    canActivate: [roleGuard],
    data: { roles: [RoleId.Driver, RoleId.SuperAdmin, RoleId.Admin] }
  },
  { 
    path: 'customer', 
    component: CustomerComponent,
    canActivate: [authGuard]
  },
  { path: 'services', component: ServicesComponent },
  { path: 'about', component: AboutComponent },
  { 
    path: 'request-service', 
    component: RequestServiceComponent,
    canActivate: [authGuard]
  },
  { path: '**', redirectTo: '' }
];
