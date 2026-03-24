import { Component, OnInit } from '@angular/core';
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
  Payment
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
export class PaymentsComponent implements OnInit {
  // Payment data
  payments: PaymentListItem[] = [];
  filteredPayments: PaymentListItem[] = [];
  paymentLinks: any[] = [];
  statistics: PaymentStatistics | null = null;
  
  // Jobs and drivers
  jobs: Job[] = [];
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
  
  // Job search for payment link
  jobSearchTerm: string = '';
  filteredJobsForLink: Job[] = [];
  
  // Filters
  searchTerm: string = '';
  paymentMethodFilter: 'Card' | 'PaymentLink' | 'Cash' | '' = '';
  paymentStatusFilter: 'Unpaid' | 'Pending' | 'PendingCash' | 'UnderReview' | 'Authorized' | 'CapturePending' | 'Paid' | 'Failed' | 'Cancelled' | '' = '';
  driverFilter: string = '';
  dateRangeStart: string = '';
  dateRangeEnd: string = '';
  showingFraudQueue = false;

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
    this.loadJobs();
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
      searchTerm: this.searchTerm || undefined
    };

    this.paymentService.getAllPayments(filters)
      .pipe(
        finalize(() => this.loading = false),
        catchError(error => {
          this.error = error.error?.message || 'Failed to load payments';
          return of([]);
        })
      )
      .subscribe(payments => {
        this.payments = payments;
        this.applyFilters();
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

  loadJobs(): void {
    this.jobService.getAllJobs()
      .pipe(
        catchError(error => {
          console.error('Failed to load jobs:', error);
          return of([]);
        })
      )
      .subscribe(jobs => {
        this.jobs = jobs.filter(job => job.cost > 0);
      });
  }

  loadDrivers(): void {
    const params = new HttpParams()
      .set('pageNumber', '1')
      .set('pageSize', '100')
      .set('isActive', 'true');

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

  applyFilters(): void {
    let filtered = [...this.payments];

    // Search filter
    if (this.searchTerm) {
      const search = this.searchTerm.toLowerCase();
      filtered = filtered.filter(p => 
        p.jobNumber.toLowerCase().includes(search) ||
        p.clientName.toLowerCase().includes(search) ||
        (p.transactionId && p.transactionId.toLowerCase().includes(search))
      );
    }

    // Payment method filter
    if (this.paymentMethodFilter) {
      filtered = filtered.filter(p => p.paymentMethod === this.paymentMethodFilter);
    }

    // Payment status filter
    if (this.paymentStatusFilter) {
      filtered = filtered.filter(p => p.paymentStatus === this.paymentStatusFilter);
    }

    // Driver filter
    if (this.driverFilter) {
      filtered = filtered.filter(p => p.driverId === this.driverFilter);
    }

    this.filteredPayments = filtered;
  }

  onFilterChange(): void {
    this.showingFraudQueue = false;
    this.loadPayments();
    this.loadStatistics();
  }

  viewFraudQueue(): void {
    this.showingFraudQueue = true;
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

  onSearchChange(): void {
    this.applyFilters();
  }

  openCreateLinkModal(): void {
    this.showCreateLinkModal = true;
    this.createPaymentLinkForm.reset({ expiresInDays: 7 });
    this.error = null;
    this.successMessage = null;
    this.selectedJobForLink = null;
    this.jobSearchTerm = '';
    // Filter to show only jobs with cost > 0
    this.filteredJobsForLink = this.jobs.filter(job => job.cost > 0);
  }

  closeCreateLinkModal(): void {
    this.showCreateLinkModal = false;
    this.createPaymentLinkForm.reset();
  }

  onJobSelectedForLink(jobId: number): void {
    const job = this.jobs.find(j => j.id === jobId);
    this.selectedJobForLink = job || null;
    this.createPaymentLinkForm.patchValue({ jobId });
  }

  filterJobsForLink(): void {
    if (!this.jobSearchTerm) {
      // Show all jobs with cost > 0
      this.filteredJobsForLink = this.jobs.filter(job => job.cost > 0);
    } else {
      const search = this.jobSearchTerm.toLowerCase();
      this.filteredJobsForLink = this.jobs.filter(job => 
        job.cost > 0 &&
        (
          job.id.toString().includes(search) ||
          (job.clientName && job.clientName.toLowerCase().includes(search)) ||
          (job.serviceType && job.serviceType.toLowerCase().includes(search))
        )
      );
    }
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
    const job = this.jobs.find(j => j.id === jobId);
    if (!job) return `Job #${jobId}`;
    return `Job #${jobId} - ${job.serviceType || 'Service'}`;
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
