import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ApiService } from './api.service';
import { AuthService } from './auth.service';
import { RoleId } from '../constants/user-roles.constants';
import { HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';

/** Full URL for a path served from the API host (e.g. job photo under wwwroot). */
export function resolvePublicAssetUrl(path: string): string {
  if (!path) return '';
  if (path.startsWith('http://') || path.startsWith('https://')) return path;
  const origin = environment.apiUrl.replace(/\/api\/?$/, '').replace(/\/$/, '');
  return `${origin}${path.startsWith('/') ? '' : '/'}${path}`;
}

export interface JobPhotoItem {
  id: number;
  url: string;
  uploadedAt: string;
}

export interface Job {
  id: number;
  status: 'Pending' | 'Assigned' | 'OnRoute' | 'InProgress' | 'ReadyToRelease' | 'Completed';
  vehicleId: number;
  vehicle?: {
    id: number;
    vin: string;
    make: string;
    model: string;
    year: number;
    color: string;
  };
  clientId?: string;
  clientName?: string;
  clientEmail?: string;
  clientPhoneNumber?: string;
  
  // Call/Job Type
  callType?: string;
  scheduledDate?: string;
  scheduledTime?: string;
  
  // Company & Account
  companyName?: string;
  account?: string;
  companyOverride?: string;
  
  // Contact Information
  contactName?: string;
  contactPhoneNumber?: string;
  
  // Location
  pickupLocation?: string;
  destinationAddress?: string;
  dropoffLocation?: string; // Alias for destinationAddress
  
  // Job Details
  reason?: string;
  priority?: string;
  invoiceNumber?: string;
  eta?: string;
  serviceType?: string;
  
  // Vehicle Details (job-specific)
  licensePlate?: string;
  licenseState?: string;
  driveType?: string;
  vehicleType?: string;
  odometer?: number;
  drivable?: string;
  haveKeys?: boolean;
  keyLocation?: string;
  
  // Assignment
  driverId?: string | null;
  driverName?: string | null;
  truckId?: string;
  
  // Financials
  cost: number;
  /** Mirrors backend Job.PaymentStatus (e.g. Unpaid, Pending, Paid). */
  paymentStatus?: string;
  paymentMethod?: string;
  paidAt?: string | null;
  notes?: string | null;
  billingNotes?: string;
  includeBillingNotesOnReceipt?: boolean;
  invoiceCharges?: InvoiceCharges;
  
  photoCount?: number;
  photos?: JobPhotoItem[];
  createdAt: string;
  completedAt?: string | null;
  statusUpdatedById?: string | null;
  statusUpdatedByName?: string | null;
  statusUpdatedAt?: string | null;
}

export interface VehicleData {
  vin: string;
  make: string;
  model: string;
  year: number;
  color?: string;
  licensePlate?: string;
  licenseState?: string;
  driveType?: string;
  vehicleType?: string;
  odometer?: number;
  drivable?: string; // "Yes" | "No"
  haveKeys?: boolean;
  keyLocation?: string;
}

export interface ClientData {
  phoneNumber: string; // Required - main key (unique identifier)
  fullName: string;
  email?: string; // Optional
  contactName?: string;
}

export interface InvoiceChargeItem {
  serviceName?: string;
  quantity: number;
  price: number;
  total: number;
}

export interface InvoiceCharges {
  unloadedEnrouteMileage?: {
    quantity: number;
    price: number;
    total: number;
  };
  loadedHookedMileage?: {
    quantity: number;
    price: number;
    total: number;
  };
  serviceItems?: InvoiceChargeItem[];
  discount?: number;
  taxExempt?: boolean;
  subtotal?: number;
  taxes?: number;
  grandTotal?: number;
}

export interface CreateJobRequest {
  // Vehicle: Provide EITHER vehicleId OR vehicle (not both, not neither)
  vehicleId?: number;
  vehicle?: VehicleData;
  
  // Client: Provide EITHER clientId OR client (not both, not neither)
  clientId?: string;
  client?: ClientData;
  
  // Call/Job Type
  callType?: string; // "New Call" | "Completed Call" | "Schedule a call" | "Quote"
  scheduledDate?: string; // ISO date string
  scheduledTime?: string; // Time string
  
  // Company & Account
  companyName?: string;
  account?: string; // Account ID or name
  companyOverride?: string; // Company name to show on invoice
  
  // Contact Information
  contactName?: string;
  contactPhoneNumber?: string;
  
  // Location
  pickupLocation?: string; // Required
  destinationAddress?: string;
  
  // Job Details
  reason?: string;
  priority?: string; // "Normal" | "Emergency" | "Low"
  invoiceNumber?: string;
  eta?: string; // DateTime string
  
  // Assignment
  driverId?: string;
  truckId?: string;
  
  // Notes
  notes?: string;
  billingNotes?: string;
  includeBillingNotesOnReceipt?: boolean;
  
  // Invoice Charges
  invoiceCharges?: InvoiceCharges;
  
  // Job data (required)
  cost: number;
  serviceType?: string;
  dropoffLocation?: string; // Alias for destinationAddress
}

export interface AssignDriverRequest {
  driverId: string;
}

/** Response from POST jobs/{id}/assign — assignment always succeeds when 200; push may be skipped. */
export interface AssignDriverResponse {
  job: Job;
  notificationSent: boolean;
  notificationMessage?: string | null;
}

export interface UpdateJobStatusRequest {
  status: 'Pending' | 'Assigned' | 'OnRoute' | 'InProgress' | 'ReadyToRelease' | 'Completed';
}

@Injectable({
  providedIn: 'root'
})
export class JobService {
  constructor(
    private apiService: ApiService,
    private authService: AuthService
  ) {}

  /** Full job list (dispatch/admin only). Drivers are routed to {@link getMyJobs} so they never see other drivers' jobs. */
  getAllJobs(status?: string): Observable<Job[]> {
    const user = this.authService.getCurrentUser();
    if (user && Number(user.roleId) === RoleId.Driver) {
      return this.getMyJobs(status);
    }

    let params = new HttpParams();
    if (status) {
      params = params.set('status', status);
    }
    return this.apiService.get<Job[]>('jobs', params).pipe(
      catchError(error => {
        console.error('Get jobs error:', error);
        return throwError(() => error);
      })
    );
  }

  /** Jobs assigned to the current driver (Driver role only). */
  getMyJobs(status?: string): Observable<Job[]> {
    let params = new HttpParams();
    if (status) {
      params = params.set('status', status);
    }
    return this.apiService.get<Job[]>('jobs/mine', params).pipe(
      catchError(error => {
        console.error('Get my jobs error:', error);
        return throwError(() => error);
      })
    );
  }

  getJobById(id: number): Observable<Job> {
    return this.apiService.get<Job>(`jobs/${id}`).pipe(
      catchError(error => {
        console.error('Get job error:', error);
        return throwError(() => error);
      })
    );
  }

  createJob(job: CreateJobRequest): Observable<Job> {
    return this.apiService.post<Job>('jobs', job).pipe(
      catchError(error => {
        console.error('Create job error:', error);
        return throwError(() => error);
      })
    );
  }

  assignDriver(jobId: number, driverId: string): Observable<AssignDriverResponse> {
    return this.apiService.post<AssignDriverResponse>(`jobs/${jobId}/assign`, { driverId }).pipe(
      catchError(error => {
        console.error('Assign driver error:', error);
        return throwError(() => error);
      })
    );
  }

  updateJobStatus(jobId: number, status: UpdateJobStatusRequest): Observable<any> {
    return this.apiService.put(`jobs/${jobId}/status`, status).pipe(
      catchError(error => {
        console.error('Update job status error:', error);
        return throwError(() => error);
      })
    );
  }

  uploadJobPhoto(jobId: number, file: File): Observable<Job> {
    const formData = new FormData();
    formData.append('file', file);
    return this.apiService.uploadFile(`jobs/${jobId}/photos`, formData).pipe(
      map((response) => response as Job),
      catchError((error) => {
        console.error('Upload job photo error:', error);
        return throwError(() => error);
      })
    );
  }
}

