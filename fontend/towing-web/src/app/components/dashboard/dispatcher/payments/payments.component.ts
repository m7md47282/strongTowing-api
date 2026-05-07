import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { HttpParams } from '@angular/common/http';
import { catchError, finalize } from 'rxjs/operators';
import { of } from 'rxjs';
import { 
  PaymentService, 
  PaymentListItem,
  PaymentStatistics,
  PaymentFilters,
  Payment,
  PagedPaymentsResponse
} from '../../../../services/payment.service';
import { JobService, Job } from '../../../../services/job.service';
import { ApiService } from '../../../../services/api.service';
import { User } from '../../../../models/user.model';

interface PagedResponse<T> {
  data: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

@Component({
  selector: 'app-payments',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './payments.component.html',
  styleUrls: ['./payments.component.scss']
})
export class PaymentsComponent implements OnInit, OnDestroy {
  Math = Math;

  // Payment data
  filteredPayments: PaymentListItem[] = [];
  paymentLinks: any[] = [];
  statistics: PaymentStatistics | null = null;
  
  drivers: User[] = [];
  
  // Loading states
  loading = false;
  loadingStatistics = false;
  error: string | null = null;
  successMessage: string | null = null;
  
  // Modals
  showCreateLinkModal = false;
  showPaymentDetailsModal = false;
  showErrorLogModal = false;
  selectedPayment: Payment | null = null;
  selectedErrorPayment: PaymentListItem | null = null;
  selectedLink: any | null = null;
  showLinkDetails = false;
  selectedJobForLink: Job | null = null;
  
  // Forms
  createPaymentLinkForm: FormGroup;
  filtersForm: FormGroup;
  submitting = false;
  
  jobSearchTerm: string = '';
  filteredJobsForLink: Job[] = [];
  loadingJobsForLink = false;

  private jobSearchDebounceTimer?: ReturnType<typeof setTimeout>;
  
  // Filters
  searchTerm: string = '';
  paymentMethodFilter: 'Card' | 'PaymentLink' | 'Cash' | '' = '';
  paymentStatusFilter: 'Unpaid' | 'Pending' | 'PendingCash' | 'UnderReview' | 'Authorized' | 'CapturePending' | 'Paid' | 'Failed' | 'Cancelled' | '' = '';
  driverFilter: string = '';
  dateRangeStart: string = '';
  dateRangeEnd: string = '';
  showingFraudQueue = false;

  /** Main list pagination (not used for fraud queue view). */
  pageNumber = 1;
  pageSize = 25;
  totalCount = 0;
  totalPages = 0;
  hasPreviousPage = false;
  hasNextPage = false;

  private paymentSearchDebounceTimer?: ReturnType<typeof setTimeout>;

  constructor(
    private paymentService: PaymentService,
    private jobService: JobService,
    private apiService: ApiService,
    private fb: FormBuilder
  ) {
    this.createPaymentLinkForm = this.fb.group({
      jobId: [''],
      expiresInDays: [7]
    });
    
    this.filtersForm = this.fb.group({
      searchTerm: [''],
      paymentMethod: [''],
      paymentStatus: [''],
      driverId: [''],
      startDate: [''],
      endDate: ['']
    });
  }

  ngOnInit(): void {
    this.loadData();
    this.setupDateRange();
  }

  ngOnDestroy(): void {
    clearTimeout(this.jobSearchDebounceTimer);
    clearTimeout(this.paymentSearchDebounceTimer);
  }

  setupDateRange(): void {
    // Default to current month
    const now = new Date();
    const firstDay = new Date(now.getFullYear(), now.getMonth(), 1);
    const lastDay = new Date(now.getFullYear(), now.getMonth() + 1, 0);
    
    this.dateRangeStart = this.formatDateForInput(firstDay);
    this.dateRangeEnd = this.formatDateForInput(lastDay);
    
    this.filtersForm.patchValue({
      startDate: this.dateRangeStart,
      endDate: this.dateRangeEnd
    });
  }

  loadData(): void {
    this.loadPayments();
    this.loadStatistics();
    this.loadDrivers();
  }

  loadPayments(): void {
    this.loading = true;
    this.error = null;

    const filters: PaymentFilters = {
      paymentMethod: this.paymentMethodFilter || undefined,
      paymentStatus: this.paymentStatusFilter || undefined,
      startDate: this.dateRangeStart || undefined,
      endDate: this.dateRangeEnd || undefined,
      driverId: this.driverFilter || undefined,
      searchTerm: this.searchTerm?.trim() || undefined,
      pageNumber: this.pageNumber,
      pageSize: this.pageSize
    };

    this.paymentService.getAllPayments(filters)
      .pipe(
        finalize(() => this.loading = false),
        catchError(error => {
          this.error = error.error?.message || 'Failed to load payments';
          const empty: PagedPaymentsResponse = {
            data: [],
            pageNumber: this.pageNumber,
            pageSize: this.pageSize,
            totalCount: 0,
            totalPages: 0,
            hasPreviousPage: false,
            hasNextPage: false
          };
          return of(empty);
        })
      )
      .subscribe(response => {
        const totalPages = response.totalPages ?? 0;
        if (response.totalCount > 0 && response.data.length === 0 && this.pageNumber > 1 && totalPages >= 1) {
          this.pageNumber = totalPages;
          this.loadPayments();
          return;
        }
        this.showingFraudQueue = false;
        this.filteredPayments = response.data ?? [];
        this.pageNumber = response.pageNumber;
        this.pageSize = response.pageSize;
        this.totalCount = response.totalCount;
        this.totalPages = response.totalPages;
        this.hasPreviousPage = response.hasPreviousPage;
        this.hasNextPage = response.hasNextPage;
      });
  }

  loadStatistics(): void {
    this.loadingStatistics = true;

    const filters = {
      startDate: this.dateRangeStart || undefined,
      endDate: this.dateRangeEnd || undefined
    };

    this.paymentService.getPaymentStatistics(filters)
      .pipe(
        finalize(() => this.loadingStatistics = false),
        catchError(error => {
          console.error('Failed to load statistics:', error);
          return of(null);
        })
      )
      .subscribe(statistics => {
        this.statistics = statistics;
      });
  }

  /** Loads jobs with positive cost from API (server-side filter + search). */
  private fetchJobsForPaymentLinkModal(): void {
    this.loadingJobsForLink = true;
    const term = this.jobSearchTerm.trim();
    this.jobService
      .getJobsPaged({
        pageNumber: 1,
        pageSize: 50,
        search: term || undefined,
        minCost: 0.01
      })
      .pipe(
        finalize(() => {
          this.loadingJobsForLink = false;
        }),
        catchError((error) => {
          console.error('Failed to load jobs for payment link:', error);
          return of({
            data: [] as Job[],
            pageNumber: 1,
            pageSize: 50,
            totalCount: 0,
            totalPages: 0,
            hasPreviousPage: false,
            hasNextPage: false
          });
        })
      )
      .subscribe((resp) => {
        this.filteredJobsForLink = resp.data || [];
      });
  }

  loadDrivers(): void {
    const params = new HttpParams()
      .set('pageNumber', '1')
      .set('pageSize', '100')
      .set('isActive', 'true')
      .set('availableForDispatchOnly', 'true');

    this.apiService.get<PagedResponse<User>>('users/drivers', params)
      .pipe(
        catchError(error => {
          console.error('Failed to load drivers:', error);
          return of({ 
            data: [], 
            pageNumber: 1, 
            pageSize: 100, 
            totalCount: 0, 
            totalPages: 0, 
            hasPreviousPage: false, 
            hasNextPage: false 
          } as PagedResponse<User>);
        })
      )
      .subscribe(response => {
        this.drivers = response.data || [];
      });
  }

  onFilterChange(): void {
    this.showingFraudQueue = false;
    this.pageNumber = 1;
    this.loadPayments();
    this.loadStatistics();
  }

  onPaymentSearchInput(): void {
    clearTimeout(this.paymentSearchDebounceTimer);
    this.paymentSearchDebounceTimer = setTimeout(() => {
      this.showingFraudQueue = false;
      this.pageNumber = 1;
      this.loadPayments();
    }, 400);
  }

  onPageSizeChange(): void {
    this.showingFraudQueue = false;
    this.pageNumber = 1;
    this.loadPayments();
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.pageNumber = page;
      this.loadPayments();
    }
  }

  nextPage(): void {
    if (this.hasNextPage) {
      this.pageNumber++;
      this.loadPayments();
    }
  }

  previousPage(): void {
    if (this.hasPreviousPage) {
      this.pageNumber--;
      this.loadPayments();
    }
  }

  getPageNumbers(): number[] {
    const pages: number[] = [];
    const maxPagesToShow = 5;
    let startPage = Math.max(1, this.pageNumber - Math.floor(maxPagesToShow / 2));
    let endPage = Math.min(this.totalPages, startPage + maxPagesToShow - 1);
    if (endPage - startPage < maxPagesToShow - 1) {
      startPage = Math.max(1, endPage - maxPagesToShow + 1);
    }
    for (let i = startPage; i <= endPage; i++) {
      pages.push(i);
    }
    return pages;
  }

  viewFraudQueue(): void {
    this.showingFraudQueue = true;
    clearTimeout(this.paymentSearchDebounceTimer);
    this.loading = true;
    this.paymentService.getFraudReviewQueue()
      .pipe(
        finalize(() => this.loading = false),
        catchError(error => {
          this.error = error.error?.message || 'Failed to load fraud review queue';
          return of([]);
        })
      )
      .subscribe(payments => {
        this.filteredPayments = payments;
      });
  }

  openCreateLinkModal(): void {
    this.showCreateLinkModal = true;
    this.createPaymentLinkForm.reset({ expiresInDays: 7 });
    this.error = null;
    this.successMessage = null;
    this.selectedJobForLink = null;
    this.jobSearchTerm = '';
    this.filteredJobsForLink = [];
    this.fetchJobsForPaymentLinkModal();
  }

  closeCreateLinkModal(): void {
    this.showCreateLinkModal = false;
    this.createPaymentLinkForm.reset();
  }

  onJobSelectedForLink(jobId: number): void {
    const job = this.filteredJobsForLink.find((j) => j.id === jobId);
    this.selectedJobForLink = job || null;
    this.createPaymentLinkForm.patchValue({ jobId });
  }

  filterJobsForLink(): void {
    clearTimeout(this.jobSearchDebounceTimer);
    this.jobSearchDebounceTimer = setTimeout(() => this.fetchJobsForPaymentLinkModal(), 300);
  }

  onCreatePaymentLink(): void {
    if (this.createPaymentLinkForm.invalid) {
      this.createPaymentLinkForm.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.error = null;
    this.successMessage = null;

    const linkData = {
      jobId: this.createPaymentLinkForm.value.jobId,
      amount: this.selectedJobForLink?.cost || 0,
      successUrl: `${window.location.origin}/dispatcher/payments`
    };

    this.paymentService.createStripePaymentLink(linkData)
      .pipe(
        finalize(() => this.submitting = false),
        catchError(error => {
          this.error = error.error?.message || 'Failed to create payment link';
          return of(null);
        })
      )
      .subscribe(link => {
        if (link) {
          this.successMessage = 'Payment link created successfully!';
          this.selectedLink = {
            id: link.paymentRecordId,
            jobId: this.createPaymentLinkForm.value.jobId,
            linkToken: link.stripePaymentLinkId,
            paymentLink: link.url,
            amount: link.amount,
            isActive: true,
            createdAt: new Date().toISOString(),
            expiresAt: null
          };
          this.showLinkDetails = true;
          this.closeCreateLinkModal();
          this.loadPayments();
          this.loadStatistics();
        }
      });
  }

  markPaymentCashPending(payment: PaymentListItem): void {
    this.paymentService.markCashPending(payment.id)
      .pipe(
        catchError(error => {
          this.error = error.error?.message || 'Failed to mark cash pending';
          return of(null);
        })
      )
      .subscribe(result => {
        if (result) {
          this.successMessage = 'Payment marked as cash pending';
          this.loadPayments();
          this.loadStatistics();
        }
      });
  }

  markPaymentCashCollected(payment: PaymentListItem): void {
    this.paymentService.markCashCollected(payment.id)
      .pipe(
        catchError(error => {
          this.error = error.error?.message || 'Failed to mark cash collected';
          return of(null);
        })
      )
      .subscribe(result => {
        if (result) {
          this.successMessage = 'Cash collection confirmed';
          this.loadPayments();
          this.loadStatistics();
        }
      });
  }

  cancelPayment(payment: PaymentListItem): void {
    if (!confirm('Cancel this payment?')) {
      return;
    }

    this.paymentService.cancelPayment(payment.id)
      .pipe(
        catchError(error => {
          this.error = error.error?.message || 'Failed to cancel payment';
          return of(null);
        })
      )
      .subscribe(result => {
        if (result) {
          this.successMessage = 'Payment cancelled';
          this.loadPayments();
          this.loadStatistics();
        }
      });
  }

  reviewPayment(payment: PaymentListItem, decision: 'approve' | 'reject'): void {
    this.paymentService.reviewPayment(payment.id, { decision })
      .pipe(
        catchError(error => {
          this.error = error.error?.message || `Failed to ${decision} payment review`;
          return of(null);
        })
      )
      .subscribe(result => {
        if (result) {
          this.successMessage = `Payment ${decision}d successfully`;
          this.loadPayments();
          this.loadStatistics();
        }
      });
  }

  captureAuthorization(payment: PaymentListItem): void {
    const amountInput = prompt('Capture amount (leave empty to capture full authorized amount):');
    const amount = amountInput ? Number(amountInput) : undefined;
    if (amountInput && (!Number.isFinite(amount) || Number(amount) <= 0)) {
      this.error = 'Invalid capture amount';
      return;
    }

    this.paymentService.captureAuthorization(payment.id, amount ? { amount } : {})
      .pipe(
        catchError(error => {
          this.error = error.error?.message || 'Failed to capture authorization';
          return of(null);
        })
      )
      .subscribe(result => {
        if (result) {
          this.successMessage = 'Authorization captured successfully';
          this.loadPayments();
          this.loadStatistics();
        }
      });
  }

  releaseAuthorization(payment: PaymentListItem): void {
    if (!confirm('Release this authorization hold?')) {
      return;
    }

    this.paymentService.releaseAuthorization(payment.id)
      .pipe(
        catchError(error => {
          this.error = error.error?.message || 'Failed to release authorization';
          return of(null);
        })
      )
      .subscribe(result => {
        if (result) {
          this.successMessage = 'Authorization released';
          this.loadPayments();
          this.loadStatistics();
        }
      });
  }

  viewPaymentDetails(payment: PaymentListItem): void {
    this.loading = true;
    this.paymentService.getPaymentById(payment.id)
      .pipe(
        finalize(() => this.loading = false),
        catchError(error => {
          this.error = error.error?.message || 'Failed to load payment details';
          return of(null);
        })
      )
      .subscribe(payment => {
        if (payment) {
          this.selectedPayment = payment;
          this.showPaymentDetailsModal = true;
        }
      });
  }

  closePaymentDetails(): void {
    this.showPaymentDetailsModal = false;
    this.selectedPayment = null;
  }

  hasPaymentError(payment: PaymentListItem): boolean {
    return !!payment.paymentErrorMessage?.trim();
  }

  openErrorLog(payment: PaymentListItem): void {
    if (!this.hasPaymentError(payment)) {
      return;
    }

    this.selectedErrorPayment = payment;
    this.showErrorLogModal = true;
  }

  closeErrorLog(): void {
    this.showErrorLogModal = false;
    this.selectedErrorPayment = null;
  }

  copyToClipboard(text: string): void {
    navigator.clipboard.writeText(text).then(() => {
      this.successMessage = 'Copied to clipboard!';
      setTimeout(() => this.successMessage = null, 3000);
    }).catch(err => {
      this.error = 'Failed to copy to clipboard';
    });
  }

  clearPaymentLink(link: any): void {
    if (!confirm('Are you sure you want to deactivate this payment link?')) {
      return;
    }

    this.paymentService.clearPaymentLink(link.linkToken)
      .pipe(
        catchError(error => {
          this.error = error.error?.message || 'Failed to clear payment link';
          return of(null);
        })
      )
      .subscribe(result => {
        if (result) {
          this.loadPayments();
          this.successMessage = 'Payment link deactivated successfully';
          setTimeout(() => this.successMessage = null, 3000);
        }
      });
  }

  getJobDisplay(jobId: number): string {
    return `Job #${jobId}`;
  }

  getDriverName(driverId?: string): string {
    if (!driverId) return 'N/A';
    const driver = this.drivers.find(d => d.id === driverId);
    return driver ? driver.fullName : 'Unknown';
  }

  formatDate(dateString: string): string {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleString();
  }

  formatDateForInput(date: Date): string {
    return date.toISOString().split('T')[0];
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  }

  getPaymentMethodIcon(method: string): string {
    switch (method) {
      case 'Card': return 'fa-credit-card';
      case 'PaymentLink': return 'fa-link';
      case 'Cash': return 'fa-money-bill';
      default: return 'fa-dollar-sign';
    }
  }

  getPaymentStatusClass(status: string): string {
    switch (status) {
      case 'Paid': return 'bg-green-100 text-green-800';
      case 'Pending': return 'bg-yellow-100 text-yellow-800';
      case 'CapturePending': return 'bg-blue-100 text-blue-800';
      case 'Authorized': return 'bg-indigo-100 text-indigo-800';
      case 'PendingCash': return 'bg-amber-100 text-amber-800';
      case 'UnderReview': return 'bg-orange-100 text-orange-800';
      case 'Failed': return 'bg-red-100 text-red-800';
      case 'Cancelled': return 'bg-gray-200 text-gray-700';
      default: return 'bg-gray-100 text-gray-800';
    }
  }
}
