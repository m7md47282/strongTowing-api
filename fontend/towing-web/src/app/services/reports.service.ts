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

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('stongTowing_token');
    const headers: { [key: string]: string } = {};
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }
    return new HttpHeaders(headers);
  }
}
