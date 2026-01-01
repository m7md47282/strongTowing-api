export enum RoleId {
  SuperAdmin = 69,
  Admin = 1,
  Dispatcher = 2,
  Driver = 3,
  User = 4
}

export interface RoleOption {
  id: RoleId;
  label: string;
}

export const USER_ROLES: readonly RoleOption[] = [
  { id: RoleId.SuperAdmin, label: 'Super Admin' },
  { id: RoleId.Admin, label: 'Administrator' },
  { id: RoleId.Dispatcher, label: 'Dispatcher' },
  { id: RoleId.Driver, label: 'Driver' },
  { id: RoleId.User, label: 'User' }
] as const;

export const ROLE_LABELS: Record<RoleId, string> = {
  [RoleId.SuperAdmin]: 'Super Admin',
  [RoleId.Admin]: 'Administrator',
  [RoleId.Dispatcher]: 'Dispatcher',
  [RoleId.Driver]: 'Driver',
  [RoleId.User]: 'User'
} as const;

export const ROLE_IDS = {
  SUPER_ADMIN: 69,
  ADMIN: 1,
  DISPATCHER: 2,
  DRIVER: 3,
  USER: 4
} as const;

