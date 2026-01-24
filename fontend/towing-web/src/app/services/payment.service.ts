import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ApiService } from './api.service';
import { HttpParams } from '@angular/common/http';

export interface PaymentLink {
  id: number;
  jobId: number;
  linkToken: string;
  paymentLink: string;
  amount: number;
  isActive: boolean;
  createdAt: string;
  expiresAt?: string | null;
}

export interface CreatePaymentLinkRequest {
  jobId: number;
  expiresInDays?: number;
}

export interface ProcessPaymentViaLinkRequest {
  paymentMethod: 'Stripe' | 'Square';
  paymentIntentId: string;
}

export interface Payment {
  id: number;
  jobId: number;
  amount: number;
  paymentMethod: 'Card' | 'PaymentLink' | 'Cash';
  paymentStatus: 'Pending' | 'Paid' | 'Failed' | 'Refunded';
  stripePaymentIntentId?: string;
  stripeChargeId?: string;
  cardLast4?: string;
  cardBrand?: string;
  paymentLinkId?: number;
  cashCollectedBy?: string;
  cashCollectedAt?: string;
  processedBy?: string;
  processedAt?: string;
  transactionId?: string;
  refundedAt?: string;
  refundReason?: string;
  refundAmount?: number;
  createdAt: string;
}

export interface PaymentListItem {
  id: number;
  jobId: number;
  jobNumber: string;
  clientName: string;
  clientEmail: string;
  amount: number;
  paymentMethod: 'Card' | 'PaymentLink' | 'Cash';
  paymentStatus: 'Pending' | 'Paid' | 'Failed';
  processedAt: string;
  processedByName: string;
  driverId?: string;
  driverName?: string;
  driverCommission: number;
  cashCollected: boolean;
  cashCollectedBy?: string;
  cashCollectedAt?: string;
  transactionId?: string;
  cardLast4?: string;
  cardBrand?: string;
}

export interface PaymentStatistics {
  totalPayments: number;
  totalRevenue: number;
  revenueByMethod: {
    card: number;
    paymentLink: number;
    cash: number;
  };
  pendingPayments: number;
  totalCashCollected: number;
  totalDriverCommissions: number;
}

export interface PaymentFilters {
  paymentMethod?: 'Card' | 'PaymentLink' | 'Cash';
  paymentStatus?: 'Pending' | 'Paid' | 'Failed';
  startDate?: string;
  endDate?: string;
  driverId?: string;
  searchTerm?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  constructor(private apiService: ApiService) {}

  createPaymentLink(request: CreatePaymentLinkRequest): Observable<PaymentLink> {
    return this.apiService.post<PaymentLink>('payments/links', request).pipe(
      catchError(error => {
        console.error('Create payment link error:', error);
        return throwError(() => error);
      })
    );
  }

  getPaymentLinksByJob(jobId: number): Observable<PaymentLink[]> {
    return this.apiService.get<PaymentLink[]>(`payments/links/job/${jobId}`).pipe(
      catchError(error => {
        console.error('Get payment links error:', error);
        return throwError(() => error);
      })
    );
  }

  getPaymentLink(linkToken: string): Observable<PaymentLink> {
    return this.apiService.get<PaymentLink>(`payments/links/${linkToken}`).pipe(
      catchError(error => {
        console.error('Get payment link error:', error);
        return throwError(() => error);
      })
    );
  }

  clearPaymentLink(linkToken: string): Observable<any> {
    return this.apiService.post(`payments/links/${linkToken}/clear`, {}).pipe(
      catchError(error => {
        console.error('Clear payment link error:', error);
        return throwError(() => error);
      })
    );
  }

  processPaymentViaLink(linkToken: string, paymentData: ProcessPaymentViaLinkRequest): Observable<any> {
    return this.apiService.post(`payments/links/${linkToken}/process`, paymentData, false).pipe(
      catchError(error => {
        console.error('Process payment via link error:', error);
        return throwError(() => error);
      })
    );
  }

  // Payment management endpoints
  getAllPayments(filters?: PaymentFilters): Observable<PaymentListItem[]> {
    let params = new HttpParams();
    if (filters) {
      if (filters.paymentMethod) params = params.set('paymentMethod', filters.paymentMethod);
      if (filters.paymentStatus) params = params.set('paymentStatus', filters.paymentStatus);
      if (filters.startDate) params = params.set('startDate', filters.startDate);
      if (filters.endDate) params = params.set('endDate', filters.endDate);
      if (filters.driverId) params = params.set('driverId', filters.driverId);
      if (filters.searchTerm) params = params.set('searchTerm', filters.searchTerm);
      if (filters.page) params = params.set('page', filters.page.toString());
      if (filters.pageSize) params = params.set('pageSize', filters.pageSize.toString());
    }
    return this.apiService.get<PaymentListItem[]>('payments', params).pipe(
      catchError(error => {
        console.error('Get payments error:', error);
        return throwError(() => error);
      })
    );
  }

  getPaymentById(paymentId: number): Observable<Payment> {
    return this.apiService.get<Payment>(`payments/${paymentId}`).pipe(
      catchError(error => {
        console.error('Get payment error:', error);
        return throwError(() => error);
      })
    );
  }

  getPaymentByJobId(jobId: number): Observable<Payment> {
    return this.apiService.get<Payment>(`payments/job/${jobId}`).pipe(
      catchError(error => {
        console.error('Get payment by job error:', error);
        return throwError(() => error);
      })
    );
  }

  getPaymentStatistics(filters?: { startDate?: string; endDate?: string }): Observable<PaymentStatistics> {
    let params = new HttpParams();
    if (filters?.startDate) params = params.set('startDate', filters.startDate);
    if (filters?.endDate) params = params.set('endDate', filters.endDate);
    return this.apiService.get<PaymentStatistics>('payments/statistics', params).pipe(
      catchError(error => {
        console.error('Get payment statistics error:', error);
        return throwError(() => error);
      })
    );
  }
}


