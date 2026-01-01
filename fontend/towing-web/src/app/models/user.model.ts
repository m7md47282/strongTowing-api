import { RoleId } from '../constants/user-roles.constants';

export interface User {
  id: string;
  email: string;
  fullName: string;
  phoneNumber: string | null;
  roleId: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface Driver extends User {
  licenseNumber: string;
  licenseExpiry: Date;
  workPermitNumber: string;
  workPermitExpiry: Date;
  profilePicture: string;
  licensePicture: string;
  workPermitPicture: string;
  isApproved: boolean;
  isAvailable: boolean;
  rating: number;
  totalJobs: number;
}

export interface Admin extends User {
  isAdmin: boolean;
  permissions: string[];
}

export interface Customer extends User {
  address: string;
  city: string;
  state: string;
  zipCode: string;
  emergencyContact: string;
  emergencyPhone: string;
}

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe?: boolean; // UI only, not sent to API
}

export interface LoginResponse {
  token: string;
  user: User;
  expiresAt: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
  roleId: number;
  phoneNumber?: string | null;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
  confirmPassword: string;
}

export interface OtpVerificationRequest {
  email: string;
  otp: string;
}
