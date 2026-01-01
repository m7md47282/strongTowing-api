import { Routes } from '@angular/router';
import { HomeComponent } from './components/home/home.component';
import { LoginComponent } from './components/auth/login/login.component';
import { RegisterComponent } from './components/auth/register/register.component';
import { ForgotPasswordComponent } from './components/auth/forgot-password/forgot-password.component';
import { AdminComponent } from './components/dashboard/admin/admin.component';
import { DashboardComponent } from './components/dashboard/admin/dashboard/dashboard.component';
import { UsersComponent } from './components/dashboard/admin/users/users.component';
import { DriverComponent } from './components/dashboard/driver/driver.component';
import { CustomerComponent } from './components/dashboard/customer/customer.component';
import { ServicesComponent } from './components/services/services.component';
import { AboutComponent } from './components/about/about.component';
import { RequestServiceComponent } from './components/request-service/request-service.component';
import { roleGuard } from './guards/role.guard';
import { RoleId } from './constants/user-roles.constants';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'home', component: HomeComponent },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'forgot-password', component: ForgotPasswordComponent },
  { 
    path: 'admin', 
    component: AdminComponent,
    canActivate: [roleGuard],
    data: { roles: [RoleId.SuperAdmin, RoleId.Admin, RoleId.Dispatcher] },
    children: [
      { path: '', component: DashboardComponent },
      { 
        path: 'users', 
        component: UsersComponent,
        canActivate: [roleGuard],
        data: { roles: [RoleId.SuperAdmin, RoleId.Admin] }
      },
      // Child routes will be added here
      // Example: { path: 'jobs', component: JobsComponent }
    ]
  },
  { 
    path: 'driver', 
    component: DriverComponent,
    canActivate: [roleGuard],
    data: { roles: [RoleId.Driver] }
  },
  { path: 'customer', component: CustomerComponent },
  { path: 'services', component: ServicesComponent },
  { path: 'about', component: AboutComponent },
  { path: 'request-service', component: RequestServiceComponent },
  { path: '**', redirectTo: '' }
];
