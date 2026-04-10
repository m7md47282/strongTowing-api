import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { ApiService } from './api.service';

export interface FinancialReportPeriod {
  startDate?: string;
  endDate?: string;
}

export interface FinancialRevenueByMethod {
  card: number;
  paymentLink: number;
  cash: number;
}

export interface FinancialStatusCounts {
  pending: number;
  paid: number;
  failed: number;
  refunded: number;
  partiallyRefunded: number;
  underReview: number;
  authorized: number;
  cancelled: number;
}

export interface FinancialReportSummary {
  totalRevenue: number;
  totalRefunded: number;
  netRevenue: number;
  totalPayments: number;
  totalJobs: number;
  averageJobCost: number;
  cancellationFeeRevenue: number;
  revenueByMethod: FinancialRevenueByMethod;
  statusCounts: FinancialStatusCounts;
  period: FinancialReportPeriod;
}

/** Admin driver payroll snapshot (matches API DriverPayrollAdminDto). */
export interface DriverPayrollAdminRow {
  id: number;
  driverId: string;
  driverName: string;
  driverEmail?: string | null;
  payPeriodStart: string;
  payPeriodEnd: string;
  totalJobs: number;
  totalJobMinutes: number;
  totalJobRevenue: number;
  commissionPercentage: number;
  grossEarnings: number;
  cashCollections: number;
  netPay: number;
  status: string;
  finalizedAt?: string | null;
  finalizedBy?: string | null;
  paidAt?: string | null;
  paidBy?: string | null;
  notes?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface GenerateDriverPayrollResponse {
  payPeriodStart: string;
  payPeriodEnd: string;
  rowsUpserted: number;
  rows: DriverPayrollAdminRow[];
}

@Injectable({
  providedIn: 'root'
})
export class ReportsService {
  private baseUrl = environment.apiUrl || 'https://localhost:7000/api';

  constructor(
    private apiService: ApiService,
    private http: HttpClient
  ) {}

  getFinancialReport(startDate?: string, endDate?: string): Observable<FinancialReportSummary> {
    let params = new HttpParams();
    if (startDate) {
      params = params.set('startDate', startDate);
    }
    if (endDate) {
      params = params.set('endDate', endDate);
    }

    return this.apiService.get<FinancialReportSummary>('reports/financial', params).pipe(
      catchError(error => {
        console.error('Get financial report error:', error);
        return throwError(() => error);
      })
    );
  }

  exportFinancialCsv(startDate?: string, endDate?: string): Observable<Blob> {
    let params = new HttpParams().set('type', 'csv');
    if (startDate) {
      params = params.set('startDate', startDate);
    }
    if (endDate) {
      params = params.set('endDate', endDate);
    }

    return this.http.get(`${this.baseUrl}/reports/export`, {
      headers: this.getAuthHeaders(),
      params,
      responseType: 'blob'
    }).pipe(
      catchError(error => {
        console.error('Export financial CSV error:', error);
        return throwError(() => error);
      })
    );
  }

  getPayrollList(startDate?: string, endDate?: string, status?: string): Observable<DriverPayrollAdminRow[]> {
    let params = new HttpParams();
    if (startDate) {
      params = params.set('startDate', startDate);
    }
    if (endDate) {
      params = params.set('endDate', endDate);
    }
    if (status) {
      params = params.set('status', status);
    }
    return this.apiService.get<DriverPayrollAdminRow[]>('reports/payroll', params).pipe(
      catchError(error => {
        console.error('Get payroll list error:', error);
        return throwError(() => error);
      })
    );
  }

  generateDriverPayroll(payPeriodStart: string, payPeriodEnd: string): Observable<GenerateDriverPayrollResponse> {
    return this.apiService
      .post<GenerateDriverPayrollResponse>('reports/payroll/generate', {
        payPeriodStart,
        payPeriodEnd
      })
      .pipe(
        catchError(error => {
          console.error('Generate payroll error:', error);
          return throwError(() => error);
        })
      );
  }

  finalizePayroll(id: number): Observable<DriverPayrollAdminRow> {
    return this.apiService.post<DriverPayrollAdminRow>(`reports/payroll/${id}/finalize`, {}).pipe(
      catchError(error => {
        console.error('Finalize payroll error:', error);
        return throwError(() => error);
      })
    );
  }

  markPayrollPaid(id: number): Observable<DriverPayrollAdminRow> {
    return this.apiService.post<DriverPayrollAdminRow>(`reports/payroll/${id}/mark-paid`, {}).pipe(
      catchError(error => {
        console.error('Mark payroll paid error:', error);
        return throwError(() => error);
      })
    );
  }

  exportPayrollCsv(startDate?: string, endDate?: string, status?: string): Observable<Blob> {
    let params = new HttpParams();
    if (startDate) {
      params = params.set('startDate', startDate);
    }
    if (endDate) {
      params = params.set('endDate', endDate);
    }
    if (status) {
      params = params.set('status', status);
    }
    return this.http.get(`${this.baseUrl}/reports/payroll/export`, {
      headers: this.getAuthHeaders(),
      params,
      responseType: 'blob'
    }).pipe(
      catchError(error => {
        console.error('Export payroll CSV error:', error);
        return throwError(() => error);
      })
    );
  }

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('stongTowing_token');
    const headers: { [key: string]: string } = {};
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }
    return new HttpHeaders(headers);
  }
}
