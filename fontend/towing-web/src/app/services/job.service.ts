import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ApiService } from './api.service';
import { HttpParams } from '@angular/common/http';

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
  notes?: string | null;
  billingNotes?: string;
  includeBillingNotesOnReceipt?: boolean;
  invoiceCharges?: InvoiceCharges;
  
  photoCount?: number;
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

export interface UpdateJobStatusRequest {
  status: 'Pending' | 'Assigned' | 'OnRoute' | 'InProgress' | 'ReadyToRelease' | 'Completed';
}

@Injectable({
  providedIn: 'root'
})
export class JobService {
  constructor(private apiService: ApiService) {}

  getAllJobs(status?: string): Observable<Job[]> {
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

  assignDriver(jobId: number, driverId: string): Observable<Job> {
    return this.apiService.post<Job>(`jobs/${jobId}/assign`, { driverId }).pipe(
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
}

