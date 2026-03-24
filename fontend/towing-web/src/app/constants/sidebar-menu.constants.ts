import { RoleId } from './user-roles.constants';

export interface MenuItem {
  label: string;
  icon: string;
  route: string;
  roles: number[]; // Role IDs that can see this item
  children?: MenuItem[];
}

export const SIDEBAR_MENU_ITEMS: MenuItem[] = [
  {
    label: 'Dashboard',
    icon: 'fas fa-home',
    route: '/admin',
    roles: [RoleId.SuperAdmin, RoleId.Admin]
  },
  {
    label: 'Dashboard',
    icon: 'fas fa-home',
    route: '/dispatcher',
    roles: [RoleId.Dispatcher]
  },
  {
    label: 'Jobs',
    icon: 'fas fa-tasks',
    route: '/admin/jobs',
    roles: [RoleId.SuperAdmin, RoleId.Admin]
  },
  {
    label: 'Drivers',
    icon: 'fas fa-id-card',
    route: '/admin/drivers',
    roles: [RoleId.SuperAdmin, RoleId.Admin]
  },
  {
    label: 'Jobs',
    icon: 'fas fa-tasks',
    route: '/dispatcher/jobs',
    roles: [RoleId.Dispatcher]
  },
  {
    label: 'Drivers',
    icon: 'fas fa-id-card',
    route: '/dispatcher/drivers',
    roles: [RoleId.Dispatcher]
  },
  {
    label: 'Users',
    icon: 'fas fa-users',
    route: '/admin/users',
    roles: [RoleId.SuperAdmin, RoleId.Admin]
  },
  {
    label: 'Accounts',
    icon: 'fas fa-building',
    route: '/admin/accounts',
    roles: [RoleId.SuperAdmin, RoleId.Admin]
  },
  {
    label: 'Vehicles',
    icon: 'fas fa-car',
    route: '/admin/vehicles',
    roles: [RoleId.SuperAdmin, RoleId.Admin]
  },
  {
    label: 'Vehicles',
    icon: 'fas fa-car',
    route: '/dispatcher/vehicles',
    roles: [RoleId.Dispatcher]
  },
  {
    label: 'Payments',
    icon: 'fas fa-credit-card',
    route: '/admin/payments',
    roles: [RoleId.SuperAdmin, RoleId.Admin]
  },
  {
    label: 'Payments',
    icon: 'fas fa-credit-card',
    route: '/dispatcher/payments',
    roles: [RoleId.Dispatcher]
  },
  {
    label: 'Reports',
    icon: 'fas fa-chart-bar',
    route: '/admin/reports',
    roles: [RoleId.SuperAdmin, RoleId.Admin],
    children: [
      {
        label: 'Financial',
        icon: 'fas fa-dollar-sign',
        route: '/admin/reports/financial',
        roles: [RoleId.SuperAdmin, RoleId.Admin]
      },
      {
        label: 'Jobs',
        icon: 'fas fa-file-alt',
        route: '/admin/reports/jobs',
        roles: [RoleId.SuperAdmin, RoleId.Admin]
      }
    ]
  },
  {
    label: 'Settings',
    icon: 'fas fa-cog',
    route: '/admin/settings',
    roles: [RoleId.SuperAdmin]
  }
];

