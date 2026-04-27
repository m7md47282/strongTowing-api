import { Injectable } from '@angular/core';
import { Observable, throwError, EMPTY } from 'rxjs';
import { catchError, map, expand, reduce } from 'rxjs/operators';
import { ApiService } from './api.service';
import { AuthService } from './auth.service';
import { RoleId } from '../constants/user-roles.constants';
import { HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';

/** Canonical job lifecycle values (matches API `JobStatus` enum names). */
export const JOB_STATUS = {
  Waiting: 'Waiting',
  Dispatch: 'Dispatch',
  OnRoute: 'OnRoute',
  OnScene: 'OnScene',
  Loaded: 'Loaded',
  Completed: 'Completed',
  Cancelled: 'Cancelled'
} as const;

export type JobStatus = (typeof JOB_STATUS)[keyof typeof JOB_STATUS];

/** Human-readable labels for tables, filters, and the full status strip. */
export const JOB_STATUS_LABELS: Record<JobStatus, string> = {
  [JOB_STATUS.Waiting]: 'Waiting',
  [JOB_STATUS.Dispatch]: 'Dispatch',
  [JOB_STATUS.OnRoute]: 'On route',
  [JOB_STATUS.OnScene]: 'On scene',
  [JOB_STATUS.Loaded]: 'Loaded',
  [JOB_STATUS.Completed]: 'Completed',
  [JOB_STATUS.Cancelled]: 'Cancelled'
};

/** Display label for a status string (see {@link normalizeJobStatus}). */
export function formatJobStatusLabel(status: string): string {
  return JOB_STATUS_LABELS[normalizeJobStatus(status)];
}

/** DB / enum integer order (0–6) — must match backend `JobStatus` declaration order. */
export const JOB_STATUS_ORDER: JobStatus[] = [
  JOB_STATUS.Waiting,
  JOB_STATUS.Dispatch,
  JOB_STATUS.OnRoute,
  JOB_STATUS.OnScene,
  JOB_STATUS.Loaded,
  JOB_STATUS.Completed,
  JOB_STATUS.Cancelled
];

/** Forward progression for dispatch/driver (excludes terminal `Cancelled`). */
export const JOB_STATUS_PIPELINE: JobStatus[] = [
  JOB_STATUS.Waiting,
  JOB_STATUS.Dispatch,
  JOB_STATUS.OnRoute,
  JOB_STATUS.OnScene,
  JOB_STATUS.Loaded,
  JOB_STATUS.Completed
];

/** Active work states before completion (Waiting → Loaded). */
export const JOB_STATUS_ACTIVE: JobStatus[] = JOB_STATUS_PIPELINE.slice(0, -1);

/** Matches API `JobBillingModes` / job `BillingPaymentMode`. */
export const JOB_BILLING_PAYMENT_MODE = {
  Standard: 'Standard',
  InsuranceFull: 'InsuranceFull',
  SplitInsuranceClient: 'SplitInsuranceClient',
  CashToDriverPayroll: 'CashToDriverPayroll'
} as const;

export type JobBillingPaymentMode =
  (typeof JOB_BILLING_PAYMENT_MODE)[keyof typeof JOB_BILLING_PAYMENT_MODE];

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
  status: JobStatus;
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
  truckId?: number | null;
  truck?: {
    id: number;
    unitLabel: string;
    truckTypeId: number;
    truckTypeName: string;
  };
  
  // Financials
  cost: number;
  /** Populated for driver responses: current commission rate from settings. */
  driverCommissionRatePercent?: number | null;
  /** Estimated commission (cost × rate / 100). */
  driverCommissionEstimate?: number | null;
  commissionVisibleToDriver?: boolean;
  /** Mirrors backend Job.PaymentStatus (e.g. Unpaid, Pending, Paid). */
  paymentStatus?: string;
  paymentMethod?: string;
  paidAt?: string | null;
  billingPaymentMode?: string;
  insuranceCoveredAmount?: number | null;
  clientCoveredAmount?: number | null;
  insurancePortionBilled?: boolean;
  clientPortionPaid?: boolean;
  driverCashCollectedAmount?: number | null;
  payrollDeductionAmount?: number | null;
  payrollDeductionRecorded?: boolean;
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

/**
 * Maps API values to canonical {@link Job} status so UI logic (e.g. status progression) matches.
 * Handles enum integers, optional `Status` vs `status`, and case-insensitive name matching.
 */
export function normalizeJobStatus(raw: string | number | undefined | null): JobStatus {
  if (raw === undefined || raw === null) {
    return JOB_STATUS.Waiting;
  }
  if (typeof raw === 'number' && Number.isInteger(raw) && raw >= 0 && raw < JOB_STATUS_ORDER.length) {
    return JOB_STATUS_ORDER[raw];
  }
  const s = String(raw).trim();
  if (!s) {
    return JOB_STATUS.Waiting;
  }
  const byLower = JOB_STATUS_ORDER.find((c) => c.toLowerCase() === s.toLowerCase());
  if (byLower) {
    return byLower;
  }
  return JOB_STATUS.Waiting;
}

/** Ensures `job.status` is set from `status` or `Status` and normalized. */
export function normalizeJob(job: Job & { Status?: string }): Job {
  const raw = job.status ?? job.Status;
  return { ...job, status: normalizeJobStatus(raw) };
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
  deadHeadMileage?: {
    quantity: number;
    price: number;
    total: number;
  };
  serviceItems?: InvoiceChargeItem[];
  discount?: number;
  discountPercent?: number;
  taxExempt?: boolean;
  hookupFee?: number;
  serviceChargePercent?: number;
  serviceChargeAmount?: number;
  taxPercent?: number;
  subtotal?: number;
  taxes?: number;
  grandTotal?: number;
  manualTotalOverride?: number;
  manualOverrideReason?: string;
  adjustedBy?: string;
  adjustedAt?: string;
  adjustmentReason?: string;
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
  truckId?: number;
  
  // Notes
  notes?: string;
  billingNotes?: string;
  includeBillingNotesOnReceipt?: boolean;
  
  // Invoice Charges
  invoiceCharges?: InvoiceCharges;
  
  // Job data (required)
  cost: number;
  serviceType?: string;
  /** Service catalog id for account+service pricing on the server. */
  servicePricingProfileId?: number;
  dropoffLocation?: string; // Alias for destinationAddress

  billingPaymentMode?: string;
  insuranceCoveredAmount?: number | null;
  clientCoveredAmount?: number | null;
}

export interface AssignDriverRequest {
  driverId: string;
}

export interface UpdateJobTruckRequest {
  truckId: number | null;
}

/** Response from POST jobs/{id}/assign — assignment always succeeds when 200; push may be skipped. */
export interface AssignDriverResponse {
  job: Job;
  notificationSent: boolean;
  notificationMessage?: string | null;
}

export interface UpdateJobStatusRequest {
  status: JobStatus;
}

export interface OverrideJobPriceRequest {
  cost: number;
  reason: string;
  commissionVisibleToDriver?: boolean;
}

export interface UpdateJobBillingPaymentRequest {
  billingPaymentMode: string;
  insuranceCoveredAmount?: number | null;
  clientCoveredAmount?: number | null;
  insurancePortionBilled: boolean;
  clientPortionPaid: boolean;
  driverCashCollectedAmount?: number | null;
  payrollDeductionAmount?: number | null;
  payrollDeductionRecorded: boolean;
}

/** API / Angular list response for paged jobs (camelCase JSON). */
export interface JobsPagedResponse {
  data: Job[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface GetJobsPagedParams {
  pageNumber: number;
  pageSize: number;
  status?: string;
  search?: string;
}

@Injectable({
  providedIn: 'root'
})
export class JobService {
  constructor(
    private apiService: ApiService,
    private authService: AuthService
  ) {}

  /**
   * Paged job list for dispatch/admin (server-side filter + search).
   * Drivers get client-paged results from {@link getMyJobs} (same rules as the jobs table search).
   */
  getJobsPaged(params: GetJobsPagedParams): Observable<JobsPagedResponse> {
    const user = this.authService.getCurrentUser();
    if (user && Number(user.roleId) === RoleId.Driver) {
      return this.getMyJobs(params.status).pipe(
        map((jobs) => this.applyDriverJobsPaging(jobs, params)),
        catchError(error => {
          console.error('Get jobs error:', error);
          return throwError(() => error);
        })
      );
    }

    let httpParams = new HttpParams()
      .set('pageNumber', String(params.pageNumber))
      .set('pageSize', String(params.pageSize));
    if (params.status) {
      httpParams = httpParams.set('status', params.status);
    }
    if (params.search?.trim()) {
      httpParams = httpParams.set('search', params.search.trim());
    }
    return this.apiService.get<JobsPagedResponse>('jobs', httpParams).pipe(
      map((resp) => ({
        ...resp,
        data: (resp.data ?? []).map((j) => normalizeJob(j as Job & { Status?: string }))
      })),
      catchError(error => {
        console.error('Get jobs error:', error);
        return throwError(() => error);
      })
    );
  }

  /**
   * Full job list by following all pages (dispatch/admin).
   * Used by dashboard, payments, driver assignments — not the main jobs table.
   * Drivers: single call to {@link getMyJobs}.
   */
  getAllJobs(status?: string): Observable<Job[]> {
    const user = this.authService.getCurrentUser();
    if (user && Number(user.roleId) === RoleId.Driver) {
      return this.getMyJobs(status);
    }

    const pageSize = 100;
    return this.getJobsPaged({ pageNumber: 1, pageSize, status }).pipe(
      expand((resp) =>
        resp.hasNextPage
          ? this.getJobsPaged({ pageNumber: resp.pageNumber + 1, pageSize, status })
          : EMPTY
      ),
      reduce(
        (acc: Job[], resp: JobsPagedResponse) => acc.concat(resp.data),
        [] as Job[]
      ),
      catchError(error => {
        console.error('Get jobs error:', error);
        return throwError(() => error);
      })
    );
  }

  private applyDriverJobsPaging(all: Job[], params: GetJobsPagedParams): JobsPagedResponse {
    let list = all;
    const raw = params.search?.trim();
    if (raw) {
      const term = raw.toLowerCase();
      list = all.filter(
        (job) =>
          job.id.toString().includes(raw) ||
          !!job.vehicle?.make?.toLowerCase().includes(term) ||
          !!job.vehicle?.model?.toLowerCase().includes(term) ||
          !!job.driverName?.toLowerCase().includes(term) ||
          !!job.serviceType?.toLowerCase().includes(term)
      );
    }

    const totalCount = list.length;
    const pageSize = Math.max(1, params.pageSize);
    const pageNumber = Math.max(1, params.pageNumber);
    const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
    const start = (pageNumber - 1) * pageSize;
    const data = list.slice(start, start + pageSize);

    return {
      data,
      pageNumber,
      pageSize,
      totalCount,
      totalPages,
      hasPreviousPage: pageNumber > 1,
      hasNextPage: pageNumber < totalPages
    };
  }

  /** Jobs assigned to the current driver (Driver role only). */
  getMyJobs(status?: string): Observable<Job[]> {
    let params = new HttpParams();
    if (status) {
      params = params.set('status', status);
    }
    return this.apiService.get<Job[]>('jobs/mine', params).pipe(
      map((jobs) => jobs.map((j) => normalizeJob(j as Job & { Status?: string }))),
      catchError(error => {
        console.error('Get my jobs error:', error);
        return throwError(() => error);
      })
    );
  }

  getJobById(id: number): Observable<Job> {
    return this.apiService.get<Job>(`jobs/${id}`).pipe(
      map((j) => normalizeJob(j as Job & { Status?: string })),
      catchError(error => {
        console.error('Get job error:', error);
        return throwError(() => error);
      })
    );
  }

  createJob(job: CreateJobRequest): Observable<Job> {
    return this.apiService.post<Job>('jobs', job).pipe(
      map((j) => normalizeJob(j as Job & { Status?: string })),
      catchError(error => {
        console.error('Create job error:', error);
        return throwError(() => error);
      })
    );
  }

  assignDriver(jobId: number, driverId: string): Observable<AssignDriverResponse> {
    return this.apiService.post<AssignDriverResponse>(`jobs/${jobId}/assign`, { driverId }).pipe(
      map((r) => ({ ...r, job: normalizeJob(r.job as Job & { Status?: string }) })),
      catchError(error => {
        console.error('Assign driver error:', error);
        return throwError(() => error);
      })
    );
  }

  updateJobTruck(jobId: number, body: UpdateJobTruckRequest): Observable<Job> {
    return this.apiService.put<Job>(`jobs/${jobId}/truck`, body).pipe(
      map((j) => normalizeJob(j as Job & { Status?: string })),
      catchError((error) => {
        console.error('Update job truck error:', error);
        return throwError(() => error);
      })
    );
  }

  updateJobStatus(jobId: number, status: UpdateJobStatusRequest): Observable<any> {
    return this.apiService.put(`jobs/${jobId}/status`, status).pipe(
      map((r) => normalizeJob(r as Job & { Status?: string })),
      catchError(error => {
        console.error('Update job status error:', error);
        return throwError(() => error);
      })
    );
  }

  /** SuperAdmin / Administrator only — updates stored job cost and invoice snapshot. */
  overrideJobPrice(jobId: number, body: OverrideJobPriceRequest): Observable<Job> {
    return this.apiService.post<Job>(`jobs/${jobId}/override-price`, body).pipe(
      map((j) => normalizeJob(j as Job & { Status?: string })),
      catchError(error => {
        console.error('Override job price error:', error);
        return throwError(() => error);
      })
    );
  }

  /** Dispatcher/admin: insurance-only, split, or cash-to-driver (payroll deduction). */
  updateJobBillingPayment(jobId: number, body: UpdateJobBillingPaymentRequest): Observable<Job> {
    return this.apiService.put<Job>(`jobs/${jobId}/billing-payment`, body).pipe(
      map((j) => normalizeJob(j as Job & { Status?: string })),
      catchError((error) => {
        console.error('Update job billing payment error:', error);
        return throwError(() => error);
      })
    );
  }

  uploadJobPhoto(jobId: number, file: File): Observable<Job> {
    const formData = new FormData();
    formData.append('file', file);
    return this.apiService.uploadFile(`jobs/${jobId}/photos`, formData).pipe(
      map((response) => normalizeJob(response as Job & { Status?: string })),
      catchError((error) => {
        console.error('Upload job photo error:', error);
        return throwError(() => error);
      })
    );
  }
}

