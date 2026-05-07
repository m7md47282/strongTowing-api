import { Routes } from '@angular/router';
import { roleGuard } from './guards/role.guard';
import { authGuard } from './guards/auth.guard';
import { guestGuard } from './guards/guest.guard';
import { RoleId } from './constants/user-roles.constants';

/** Shared lazy loaders — identical import() strings dedupe into one chunk. */
const loadDashboard = () =>
  import('./components/dashboard/admin/dashboard/dashboard.component').then(
    (m) => m.DashboardComponent
  );
const loadJobs = () =>
  import('./components/dashboard/dispatcher/jobs/jobs.component').then((m) => m.JobsComponent);
const loadDriverAssignments = () =>
  import('./components/dashboard/shared/driver-assignments/driver-assignments.component').then(
    (m) => m.DriverAssignmentsComponent
  );
const loadVehicles = () =>
  import('./components/dashboard/dispatcher/vehicles/vehicles.component').then(
    (m) => m.VehiclesComponent
  );
const loadTrucks = () =>
  import('./components/dashboard/dispatcher/trucks/trucks.component').then((m) => m.TrucksComponent);
const loadPayments = () =>
  import('./components/dashboard/dispatcher/payments/payments.component').then(
    (m) => m.PaymentsComponent
  );
const loadAdminInvoices = () =>
  import('./components/dashboard/admin/invoices/invoices.component').then(
    (m) => m.AdminInvoicesComponent
  );
const loadAccountCashCall = () =>
  import('./components/dashboard/shared/account-cash-call-rates/account-cash-call-rates.component').then(
    (m) => m.AccountCashCallRatesComponent
  );
const loadLegalDocument = () =>
  import('./components/legal/legal-document.component').then((m) => m.LegalDocumentComponent);

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/home/home.component').then((m) => m.HomeComponent),
    canActivate: [guestGuard],
  },
  {
    path: 'home',
    loadComponent: () =>
      import('./components/home/home.component').then((m) => m.HomeComponent),
    canActivate: [guestGuard],
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./components/auth/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./components/auth/register/register.component').then((m) => m.RegisterComponent),
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./components/auth/forgot-password/forgot-password.component').then(
        (m) => m.ForgotPasswordComponent
      ),
  },
  {
    path: 'invoice-print/:id',
    loadComponent: () =>
      import('./components/dashboard/shared/invoice-print/invoice-print.component').then(
        (m) => m.InvoicePrintComponent
      ),
    canActivate: [authGuard, roleGuard],
    data: { roles: [RoleId.SuperAdmin, RoleId.Admin, RoleId.Dispatcher] },
  },
  {
    path: 'admin',
    loadComponent: () =>
      import('./components/dashboard/admin/admin.component').then((m) => m.AdminComponent),
    canActivate: [roleGuard, authGuard],
    data: { roles: [RoleId.SuperAdmin, RoleId.Admin] },
    children: [
      { path: '', loadComponent: loadDashboard },
      {
        path: 'users',
        loadComponent: () =>
          import('./components/dashboard/admin/users/users.component').then((m) => m.UsersComponent),
        canActivate: [roleGuard, authGuard],
        data: { roles: [RoleId.SuperAdmin, RoleId.Admin] },
      },
      {
        path: 'accounts',
        loadComponent: () =>
          import('./components/dashboard/admin/accounts/accounts.component').then(
            (m) => m.AccountsComponent
          ),
        canActivate: [roleGuard, authGuard],
        data: { roles: [RoleId.SuperAdmin, RoleId.Admin] },
      },
      {
        path: 'services',
        loadComponent: () =>
          import('./components/dashboard/admin/services/services.component').then(
            (m) => m.ServicesComponent
          ),
        canActivate: [roleGuard, authGuard],
        data: { roles: [RoleId.SuperAdmin, RoleId.Admin] },
      },
      {
        path: 'drivers',
        loadComponent: loadDriverAssignments,
        canActivate: [roleGuard, authGuard],
        data: { roles: [RoleId.SuperAdmin, RoleId.Admin] },
      },
      {
        path: 'jobs',
        loadComponent: loadJobs,
        canActivate: [roleGuard, authGuard],
        data: { roles: [RoleId.SuperAdmin, RoleId.Admin] },
      },
      {
        path: 'vehicles',
        loadComponent: loadVehicles,
        canActivate: [roleGuard, authGuard],
        data: { roles: [RoleId.SuperAdmin, RoleId.Admin] },
      },
      {
        path: 'trucks',
        loadComponent: loadTrucks,
        canActivate: [roleGuard, authGuard],
        data: { roles: [RoleId.SuperAdmin, RoleId.Admin] },
      },
      {
        path: 'payments',
        loadComponent: loadPayments,
        canActivate: [roleGuard, authGuard],
        data: { roles: [RoleId.SuperAdmin, RoleId.Admin] },
      },
      {
        path: 'invoices',
        loadComponent: loadAdminInvoices,
        canActivate: [roleGuard, authGuard],
        data: { roles: [RoleId.SuperAdmin, RoleId.Admin] },
      },
      {
        path: 'reports',
        redirectTo: 'reports/financial',
        pathMatch: 'full',
      },
      {
        path: 'reports/financial',
        loadComponent: () =>
          import('./components/dashboard/admin/reports/financial-report/financial-report.component').then(
            (m) => m.FinancialReportComponent
          ),
        canActivate: [roleGuard, authGuard],
        data: { roles: [RoleId.SuperAdmin, RoleId.Admin] },
      },
      {
        path: 'reports/payroll',
        loadComponent: () =>
          import('./components/dashboard/admin/reports/payroll-report/payroll-report.component').then(
            (m) => m.PayrollReportComponent
          ),
        canActivate: [roleGuard, authGuard],
        data: { roles: [RoleId.SuperAdmin, RoleId.Admin] },
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('./components/dashboard/admin/settings/settings.component').then(
            (m) => m.SettingsComponent
          ),
        canActivate: [roleGuard, authGuard],
        data: { roles: [RoleId.SuperAdmin, RoleId.Admin] },
      },
      {
        path: 'accounts/:accountId/cash-call',
        loadComponent: loadAccountCashCall,
        canActivate: [roleGuard, authGuard],
        data: { roles: [RoleId.SuperAdmin, RoleId.Admin] },
      },
    ],
  },
  {
    path: 'dispatcher',
    loadComponent: () =>
      import('./components/dashboard/dispatcher/dispatcher.component').then(
        (m) => m.DispatcherComponent
      ),
    canActivate: [roleGuard, authGuard],
    data: { roles: [RoleId.Dispatcher, RoleId.SuperAdmin, RoleId.Admin] },
    children: [
      { path: '', loadComponent: loadDashboard },
      { path: 'jobs', loadComponent: loadJobs },
      { path: 'drivers', loadComponent: loadDriverAssignments },
      { path: 'vehicles', loadComponent: loadVehicles },
      { path: 'trucks', loadComponent: loadTrucks },
      { path: 'payments', loadComponent: loadPayments },
      { path: 'invoices', loadComponent: loadAdminInvoices },
      { path: 'accounts/:accountId/cash-call', loadComponent: loadAccountCashCall },
    ],
  },
  {
    path: 'driver',
    loadComponent: () =>
      import('./components/dashboard/driver/driver.component').then((m) => m.DriverComponent),
    canActivate: [roleGuard, authGuard],
    data: { roles: [RoleId.Driver, RoleId.SuperAdmin, RoleId.Admin] },
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./components/dashboard/driver/driver-home/driver-home.component').then(
            (m) => m.DriverHomeComponent
          ),
      },
      {
        path: 'jobs',
        loadComponent: () =>
          import('./components/dashboard/driver/driver-jobs/driver-jobs.component').then(
            (m) => m.DriverJobsComponent
          ),
      },
      {
        path: 'jobs/:id',
        loadComponent: () =>
          import('./components/dashboard/driver/driver-job-detail/driver-job-detail.component').then(
            (m) => m.DriverJobDetailComponent
          ),
      },
      {
        path: 'profile',
        loadComponent: () =>
          import('./components/dashboard/driver/driver-profile/driver-profile.component').then(
            (m) => m.DriverProfileComponent
          ),
      },
      {
        path: 'earnings',
        loadComponent: () =>
          import('./components/dashboard/driver/driver-earnings/driver-earnings.component').then(
            (m) => m.DriverEarningsComponent
          ),
      },
    ],
  },
  {
    path: 'customer',
    loadComponent: () =>
      import('./components/dashboard/customer/customer.component').then((m) => m.CustomerComponent),
    canActivate: [authGuard],
  },
  {
    path: 'services',
    loadComponent: () =>
      import('./components/services/services.component').then((m) => m.ServicesComponent),
  },
  {
    path: 'about',
    loadComponent: () =>
      import('./components/about/about.component').then((m) => m.AboutComponent),
  },
  {
    path: 'request-service',
    loadComponent: () =>
      import('./components/request-service/request-service.component').then(
        (m) => m.RequestServiceComponent
      ),
  },
  {
    path: 'map-calculator',
    loadComponent: () =>
      import('./components/shared/location-picker/location-picker.component').then(
        (m) => m.LocationPickerComponent
      ),
  },
  {
    path: 'sms-consent',
    loadComponent: loadLegalDocument,
    data: { legalPageId: 'sms-consent' },
  },
  {
    path: 'privacy-policy',
    loadComponent: loadLegalDocument,
    data: { legalPageId: 'privacy-policy' },
  },
  {
    path: 'terms-of-service',
    loadComponent: loadLegalDocument,
    data: { legalPageId: 'terms-of-service' },
  },
  {
    path: 'cancellation-policy',
    loadComponent: loadLegalDocument,
    data: { legalPageId: 'cancellation-policy' },
  },
  { path: '**', redirectTo: '' },
];
