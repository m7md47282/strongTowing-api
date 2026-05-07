import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ApiService } from './api.service';
import { HttpParams } from '@angular/common/http';
import { loadStripe, Stripe } from '@stripe/stripe-js';

// ─── Existing interfaces ──────────────────────────────────────────────────────

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
  paymentStatus: 'Unpaid' | 'Pending' | 'PendingCash' | 'UnderReview' | 'Authorized' | 'CapturePending' | 'Paid' | 'Failed' | 'Cancelled' | 'Refunded' | 'PartiallyRefunded';
  captureStatus?: 'NotApplicable' | 'PendingAuthorization' | 'Authorized' | 'Captured' | 'PartiallyCaptured' | 'Released';
  isPreAuthorization?: boolean;
  authorizedAmount?: number;
  capturedAmount?: number;
  authorizationExpiresAt?: string;
  capturedAt?: string;
  releasedAt?: string;
  fraudStatus?: 'Clear' | 'UnderReview' | 'Approved' | 'Rejected';
  fraudScore?: number;
  fraudReasons?: string;
  fraudReviewedBy?: string;
  fraudReviewedAt?: string;
  isCancellationFeePayment?: boolean;
  cancellationFeeAmount?: number;
  stripePaymentIntentId?: string;
  stripeChargeId?: string;
  cardLast4?: string;
  cardBrand?: string;
  paymentLinkId?: number;
  stripePaymentLinkId?: string;
  stripePaymentLinkUrl?: string;
  cashCollectedBy?: string;
  cashCollectedAt?: string;
  processedBy?: string;
  processedAt?: string;
  transactionId?: string;
  paymentErrorMessage?: string;
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
  paymentStatus: 'Unpaid' | 'Pending' | 'PendingCash' | 'UnderReview' | 'Authorized' | 'CapturePending' | 'Paid' | 'Failed' | 'Cancelled' | 'Refunded' | 'PartiallyRefunded';
  captureStatus?: 'NotApplicable' | 'PendingAuthorization' | 'Authorized' | 'Captured' | 'PartiallyCaptured' | 'Released';
  isPreAuthorization?: boolean;
  authorizedAmount?: number;
  capturedAmount?: number;
  authorizationExpiresAt?: string;
  processedAt: string;
  processedByName: string;
  driverId?: string;
  driverName?: string;
  driverCommission: number;
  driverCommissionRatePercent?: number;
  cashCollected: boolean;
  cashCollectedBy?: string;
  cashCollectedAt?: string;
  transactionId?: string;
  cardLast4?: string;
  cardBrand?: string;
  paymentErrorMessage?: string;
  fraudStatus?: 'Clear' | 'UnderReview' | 'Approved' | 'Rejected';
  fraudScore?: number;
  isCancellationFeePayment?: boolean;
  cancellationFeeAmount?: number;
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
  driverCommissionRatePercent?: number;
}

export interface PaymentFilters {
  paymentMethod?: 'Card' | 'PaymentLink' | 'Cash';
  paymentStatus?: 'Unpaid' | 'Pending' | 'PendingCash' | 'UnderReview' | 'Authorized' | 'CapturePending' | 'Paid' | 'Failed' | 'Cancelled' | 'Refunded' | 'PartiallyRefunded';
  startDate?: string;
  endDate?: string;
  driverId?: string;
  searchTerm?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface PagedPaymentsResponse {
  data: PaymentListItem[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

// ─── Stripe-specific interfaces ───────────────────────────────────────────────

export interface CreatePaymentIntentRequest {
  jobId: number;
  amount: number;
  currency?: string;
  manualCapture?: boolean;
}

export interface CreatePaymentIntentResponse {
  clientSecret: string;
  paymentIntentId: string;
  publishableKey: string;
  amount: number;
  currency: string;
  manualCapture: boolean;
  capturableAmount: number;
  authorizationExpiresAt?: string;
  riskLevel?: string;
}

export interface CreateStripePaymentLinkRequest {
  jobId: number;
  amount: number;
  successUrl?: string;
}

export interface CreateStripePaymentLinkResponse {
  url: string;
  stripePaymentLinkId: string;
  paymentRecordId: number;
  amount: number;
}

export interface RefundPaymentRequest {
  amount?: number;
  reason?: string;
}

export interface RefundPaymentResponse {
  refundId: string;
  amount: number;
  status: string;
  paymentId: number;
  reason?: string;
}

export interface ReviewPaymentRequest {
  decision: 'approve' | 'reject';
  notes?: string;
}

export interface CaptureAuthorizationRequest {
  amount?: number;
}

@Injectable({
  providedIn: 'root'
})
export class PaymentService {

  /** Cached Stripe.js instance — loaded once per publishable key. */
  private stripeInstance: Stripe | null = null;
  private stripePublishableKey: string | null = null;

  constructor(private apiService: ApiService) {}

  // ─── Stripe: Payment Intent ──────────────────────────────────────────────

  /**
   * Ask the backend to create a Stripe PaymentIntent.
   * Returns the clientSecret and publishableKey needed to confirm the payment on the frontend.
   */
  createPaymentIntent(request: CreatePaymentIntentRequest): Observable<CreatePaymentIntentResponse> {
    return this.apiService.post<CreatePaymentIntentResponse>('payments/create-payment-intent', request).pipe(
      catchError(error => {
        console.error('Create payment intent error:', error);
        return throwError(() => error);
      })
    );
  }

  /**
   * Load and cache the Stripe.js SDK instance for a given publishable key.
   * Call this after receiving a CreatePaymentIntentResponse to confirm the payment.
   */
  async getStripe(publishableKey: string): Promise<Stripe | null> {
    if (this.stripeInstance && this.stripePublishableKey === publishableKey) {
      return this.stripeInstance;
    }
    this.stripeInstance = await loadStripe(publishableKey);
    this.stripePublishableKey = publishableKey;
    return this.stripeInstance;
  }

  // ─── Stripe: Payment Links ───────────────────────────────────────────────

  /**
   * Ask the backend to create a Stripe-hosted Payment Link for a job.
   * The returned URL can be shared with the customer.
   */
  createStripePaymentLink(request: CreateStripePaymentLinkRequest): Observable<CreateStripePaymentLinkResponse> {
    return this.apiService.post<CreateStripePaymentLinkResponse>('payments/create-payment-link', request).pipe(
      catchError(error => {
        console.error('Create Stripe payment link error:', error);
        return throwError(() => error);
      })
    );
  }

  // ─── Stripe: Refunds ─────────────────────────────────────────────────────

  /**
   * Issue a full or partial refund for a payment through Stripe.
   * Only Admin users can issue refunds.
   */
  refundPayment(paymentId: number, request: RefundPaymentRequest = {}): Observable<RefundPaymentResponse> {
    return this.apiService.post<RefundPaymentResponse>(`payments/${paymentId}/refund`, request).pipe(
      catchError(error => {
        console.error('Refund payment error:', error);
        return throwError(() => error);
      })
    );
  }

  markCashPending(paymentId: number): Observable<Payment> {
    return this.apiService.post<Payment>(`payments/${paymentId}/mark-cash-pending`, {}).pipe(
      catchError(error => {
        console.error('Mark cash pending error:', error);
        return throwError(() => error);
      })
    );
  }

  markCashCollected(paymentId: number): Observable<Payment> {
    return this.apiService.post<Payment>(`payments/${paymentId}/mark-cash-collected`, {}).pipe(
      catchError(error => {
        console.error('Mark cash collected error:', error);
        return throwError(() => error);
      })
    );
  }

  cancelPayment(paymentId: number): Observable<Payment> {
    return this.apiService.post<Payment>(`payments/${paymentId}/cancel`, {}).pipe(
      catchError(error => {
        console.error('Cancel payment error:', error);
        return throwError(() => error);
      })
    );
  }

  getFraudReviewQueue(): Observable<PaymentListItem[]> {
    return this.apiService.get<PaymentListItem[]>('payments/fraud-review-queue').pipe(
      catchError(error => {
        console.error('Get fraud review queue error:', error);
        return throwError(() => error);
      })
    );
  }

  reviewPayment(paymentId: number, request: ReviewPaymentRequest): Observable<Payment> {
    return this.apiService.post<Payment>(`payments/${paymentId}/review-decision`, request).pipe(
      catchError(error => {
        console.error('Review payment error:', error);
        return throwError(() => error);
      })
    );
  }

  captureAuthorization(paymentId: number, request: CaptureAuthorizationRequest = {}): Observable<Payment> {
    return this.apiService.post<Payment>(`payments/${paymentId}/capture-authorization`, request).pipe(
      catchError(error => {
        console.error('Capture authorization error:', error);
        return throwError(() => error);
      })
    );
  }

  releaseAuthorization(paymentId: number): Observable<Payment> {
    return this.apiService.post<Payment>(`payments/${paymentId}/release-authorization`, {}).pipe(
      catchError(error => {
        console.error('Release authorization error:', error);
        return throwError(() => error);
      })
    );
  }

  // ─── Existing payment management endpoints ───────────────────────────────

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

  getAllPayments(filters?: PaymentFilters): Observable<PagedPaymentsResponse> {
    let params = new HttpParams();
    if (filters) {
      if (filters.paymentMethod) params = params.set('paymentMethod', filters.paymentMethod);
      if (filters.paymentStatus) params = params.set('paymentStatus', filters.paymentStatus);
      if (filters.startDate) params = params.set('startDate', filters.startDate);
      if (filters.endDate) params = params.set('endDate', filters.endDate);
      if (filters.driverId) params = params.set('driverId', filters.driverId);
      if (filters.searchTerm) params = params.set('searchTerm', filters.searchTerm);
      if (filters.pageNumber != null) params = params.set('pageNumber', filters.pageNumber.toString());
      if (filters.pageSize != null) params = params.set('pageSize', filters.pageSize.toString());
    }
    return this.apiService.get<PagedPaymentsResponse>('payments', params).pipe(
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
