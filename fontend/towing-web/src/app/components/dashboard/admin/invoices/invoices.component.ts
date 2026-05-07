import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs/operators';
import { InvoiceService } from '../../../../services/invoice.service';
import { AuthService } from '../../../../services/auth.service';
import { CreateInvoicePayload, InvoiceDetail, InvoiceListItem } from '../../../../models/invoice.model';

@Component({
  selector: 'app-admin-invoices',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './invoices.component.html',
  styleUrl: './invoices.component.scss'
})
export class AdminInvoicesComponent implements OnInit {
  invoices: InvoiceListItem[] = [];
  loading = false;
  error: string | null = null;
  successMessage: string | null = null;

  searchTerm = '';
  statusFilter: string = 'All';

  page = 1;
  pageSize = 20;
  totalCount = 0;
  totalPages = 0;
  hasPreviousPage = false;
  hasNextPage = false;

  readonly pageSizeOptions = [10, 20, 50, 100];

  detailOpen = false;
  detailLoading = false;
  selectedDetail: InvoiceDetail | null = null;

  readonly statusOptions = ['All', 'Draft', 'Sent', 'Paid', 'Cancelled'];
  readonly invoiceStatusOptions = ['Draft', 'Sent', 'Paid', 'Cancelled'];

  createModalOpen = false;
  createSubmitting = false;
  createError: string | null = null;
  createForm: FormGroup;

  constructor(
    private invoiceService: InvoiceService,
    private authService: AuthService,
    private fb: FormBuilder
  ) {
    this.createForm = this.fb.group({
      issuedDate: [this.todayIsoDate(), Validators.required],
      dueDate: [''],
      clientName: ['', [Validators.required, Validators.maxLength(200)]],
      clientPhone: [''],
      clientEmail: [''],
      clientAddress: [''],
      jobId: [''],
      taxRate: [7.5, [Validators.required, Validators.min(0), Validators.max(100)]],
      status: ['Draft', Validators.required],
      notes: [''],
      lineItems: this.fb.array([this.newLineItemGroup()])
    });
  }

  ngOnInit(): void {
    this.load();
  }

  get canDeleteInvoices(): boolean {
    return this.authService.isAdmin();
  }

  openPrintInNewTab(invoiceId: number): void {
    const origin = typeof window !== 'undefined' ? window.location.origin : '';
    window.open(`${origin}/invoice-print/${invoiceId}`, '_blank', 'noopener');
  }

  markInvoicePaid(inv: InvoiceDetail): void {
    this.invoiceService.updateStatus(inv.id, 'Paid').subscribe({
      next: () => {
        this.selectedDetail = { ...inv, status: 'Paid' };
        this.successMessage = `Invoice ${inv.invoiceNumber} marked as Paid.`;
        setTimeout(() => (this.successMessage = null), 4000);
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.error = this.httpErrorMessage(err);
      }
    });
  }

  deleteInvoice(inv: InvoiceDetail): void {
    if (!this.canDeleteInvoices) {
      return;
    }
    const ok = confirm(`Delete invoice ${inv.invoiceNumber}? This cannot be undone.`);
    if (!ok) {
      return;
    }
    this.invoiceService.delete(inv.id).subscribe({
      next: () => {
        this.closeDetail();
        this.successMessage = 'Invoice deleted.';
        setTimeout(() => (this.successMessage = null), 4000);
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.error = this.httpErrorMessage(err);
      }
    });
  }

  get lineItems(): FormArray {
    return this.createForm.get('lineItems') as FormArray;
  }

  private todayIsoDate(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private newLineItemGroup(): FormGroup {
    return this.fb.group({
      serviceName: ['', [Validators.maxLength(200)]],
      details: [''],
      category: ['Services'],
      unitType: [''],
      unitPrice: [0, [Validators.required, Validators.min(0)]],
      quantity: [1, [Validators.required, Validators.min(0.01)]],
      discount: [0, [Validators.min(0)]],
      isDiscountPercentage: [false],
      isTaxable: [true],
      sortOrder: [0]
    });
  }

  load(): void {
    this.loading = true;
    this.error = null;
    this.invoiceService
      .list({
        page: this.page,
        pageSize: this.pageSize,
        search: this.searchTerm.trim() || undefined,
        status: this.statusFilter
      })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (res) => {
          this.invoices = res.data;
          this.page = res.pageNumber;
          this.pageSize = res.pageSize;
          this.totalCount = res.totalCount;
          this.totalPages = res.totalPages;
          this.hasPreviousPage = res.hasPreviousPage;
          this.hasNextPage = res.hasNextPage;
        },
        error: (err: HttpErrorResponse) => {
          this.error = this.httpErrorMessage(err);
        }
      });
  }

  applyFilters(): void {
    this.page = 1;
    this.totalCount = 0;
    this.totalPages = 0;
    this.hasPreviousPage = false;
    this.hasNextPage = false;
    this.invoices = [];
    this.load();
  }

  prevPage(): void {
    if (this.hasPreviousPage) {
      this.page--;
      this.load();
    }
  }

  nextPage(): void {
    if (this.hasNextPage) {
      this.page++;
      this.load();
    }
  }

  goToFirstPage(): void {
    if (this.page > 1) {
      this.page = 1;
      this.load();
    }
  }

  goToLastPage(): void {
    if (this.totalPages > 0 && this.page < this.totalPages) {
      this.page = this.totalPages;
      this.load();
    }
  }

  onPageSizeChange(): void {
    this.page = 1;
    this.load();
  }

  get rangeStart(): number {
    if (this.totalCount === 0) {
      return 0;
    }
    return (this.page - 1) * this.pageSize + 1;
  }

  get rangeEnd(): number {
    if (this.totalCount === 0) {
      return 0;
    }
    return Math.min(this.page * this.pageSize, this.totalCount);
  }

  get showPagination(): boolean {
    return this.totalCount > 0;
  }

  openCreateModal(): void {
    this.createError = null;
    this.createForm.patchValue({
      issuedDate: this.todayIsoDate(),
      dueDate: '',
      clientName: '',
      clientPhone: '',
      clientEmail: '',
      clientAddress: '',
      jobId: '',
      taxRate: 7.5,
      status: 'Draft',
      notes: ''
    });
    while (this.lineItems.length) {
      this.lineItems.removeAt(0);
    }
    this.lineItems.push(this.newLineItemGroup());
    this.createModalOpen = true;
  }

  closeCreateModal(): void {
    this.createModalOpen = false;
    this.createSubmitting = false;
    this.createError = null;
  }

  addLineItem(): void {
    this.lineItems.push(this.newLineItemGroup());
  }

  removeLineItem(index: number): void {
    if (this.lineItems.length <= 1) {
      return;
    }
    this.lineItems.removeAt(index);
  }

  submitCreate(): void {
    this.createError = null;
    this.createForm.markAllAsTouched();
    if (this.createForm.invalid) {
      this.createError = 'Please fix the highlighted fields.';
      return;
    }

    const raw = this.createForm.getRawValue();
    const lineRows = raw.lineItems as Record<string, unknown>[];
    const lineItems = lineRows
      .map((li, idx) => ({
        serviceName: String(li['serviceName'] ?? '').trim(),
        details: String(li['details'] ?? '').trim() || null,
        category: String(li['category'] ?? 'Services').trim() || 'Services',
        unitType: String(li['unitType'] ?? '').trim() || null,
        unitPrice: Number(li['unitPrice']) || 0,
        quantity: Number(li['quantity']) || 0,
        discount: Number(li['discount']) || 0,
        isDiscountPercentage: Boolean(li['isDiscountPercentage']),
        isTaxable: Boolean(li['isTaxable']),
        sortOrder: idx
      }))
      .filter((li) => li.serviceName.length > 0);

    if (lineItems.length === 0) {
      this.createError = 'Add at least one line item with a service name.';
      return;
    }

    const invalidQty = lineItems.some((li) => li.quantity <= 0 || li.unitPrice < 0);
    if (invalidQty) {
      this.createError = 'Each line item needs quantity greater than 0 and a non-negative unit price.';
      return;
    }

    const jobRaw = String(raw.jobId ?? '').trim();
    const jobIdParsed = jobRaw === '' ? null : Number(jobRaw);
    const jobIdPayload =
      jobIdParsed != null && Number.isFinite(jobIdParsed) && jobIdParsed > 0 ? jobIdParsed : null;

    const dueStr = String(raw.dueDate ?? '').trim();
    const issuedStr = String(raw.issuedDate ?? '').trim();

    const payload: CreateInvoicePayload = {
      issuedDate: issuedStr ? `${issuedStr}T12:00:00.000Z` : new Date().toISOString(),
      dueDate: dueStr ? `${dueStr}T12:00:00.000Z` : null,
      clientName: String(raw.clientName ?? '').trim(),
      clientPhone: String(raw.clientPhone ?? '').trim() || null,
      clientEmail: String(raw.clientEmail ?? '').trim() || null,
      clientAddress: String(raw.clientAddress ?? '').trim() || null,
      jobId: jobIdPayload,
      taxRate: Number(raw.taxRate) ?? 7.5,
      notes: String(raw.notes ?? '').trim() || null,
      status: String(raw.status ?? 'Draft'),
      lineItems
    };

    this.createSubmitting = true;
    this.invoiceService.create(payload).subscribe({
      next: (inv) => {
        this.createSubmitting = false;
        this.closeCreateModal();
        this.successMessage = `Invoice ${inv.invoiceNumber} created.`;
        setTimeout(() => (this.successMessage = null), 5000);
        this.page = 1;
        this.load();
        this.showCreatedDetail(inv);
      },
      error: (err: HttpErrorResponse) => {
        this.createSubmitting = false;
        this.createError = this.createFormHttpError(err);
      }
    });
  }

  private showCreatedDetail(inv: InvoiceDetail): void {
    this.detailOpen = true;
    this.detailLoading = false;
    this.selectedDetail = inv;
  }

  openDetail(row: InvoiceListItem): void {
    this.detailOpen = true;
    this.detailLoading = true;
    this.selectedDetail = null;
    this.error = null;
    this.invoiceService.getById(row.id).subscribe({
      next: (inv) => {
        this.selectedDetail = inv;
        this.detailLoading = false;
      },
      error: (err: HttpErrorResponse) => {
        this.detailLoading = false;
        this.error = this.httpErrorMessage(err);
        this.closeDetail();
      }
    });
  }

  closeDetail(): void {
    this.detailOpen = false;
    this.selectedDetail = null;
    this.detailLoading = false;
  }

  private createFormHttpError(err: HttpErrorResponse): string {
    const e = err.error;
    if (e && typeof e === 'object' && 'errors' in e) {
      const bag = (e as { errors: Record<string, unknown> }).errors;
      const parts: string[] = [];
      for (const key of Object.keys(bag)) {
        const v = bag[key];
        if (Array.isArray(v)) {
          parts.push(...v.map((x) => String(x)));
        } else if (typeof v === 'string') {
          parts.push(v);
        }
      }
      if (parts.length) {
        return parts.join(' ');
      }
    }
    return this.httpErrorMessage(err);
  }

  private httpErrorMessage(err: HttpErrorResponse): string {
    const e = err.error;
    if (e && typeof e === 'object' && 'message' in e && typeof (e as { message?: string }).message === 'string') {
      return (e as { message: string }).message;
    }
    if (typeof e === 'string' && e.length > 0) {
      return e;
    }
    return err.message || 'Request failed.';
  }
}
