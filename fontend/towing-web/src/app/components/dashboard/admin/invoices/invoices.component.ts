import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs/operators';
import { InvoiceService } from '../../../../services/invoice.service';
import { AuthService } from '../../../../services/auth.service';
import { resolvePublicAssetUrl } from '../../../../services/job.service';
import {
  CreateInvoicePayload,
  InvoiceDetail,
  InvoiceImage,
  InvoiceListItem
} from '../../../../models/invoice.model';

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
  createUploadingImages = false;
  createError: string | null = null;
  createForm: FormGroup;

  /** Queued files for the New invoice modal; uploaded right after POST /invoices succeeds. */
  createPendingImages: { id: number; file: File; objectUrl: string }[] = [];
  private createPendingImageIdSeq = 0;

  /** Reuse the same form for PUT /invoices/{id}. */
  isEditMode = false;
  editingInvoiceId: number | null = null;
  editingInvoiceNumber = '';

  imageUploading = false;
  imageError: string | null = null;
  readonly maxInvoiceImages = 20;

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
    this.isEditMode = false;
    this.editingInvoiceId = null;
    this.editingInvoiceNumber = '';
    this.clearCreatePendingImages();
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
    this.createUploadingImages = false;
  }

  onCreateModalBackdropClick(): void {
    if (this.createSubmitting) {
      return;
    }
    this.closeCreateModal();
  }

  closeCreateModal(): void {
    this.createModalOpen = false;
    this.createSubmitting = false;
    this.createUploadingImages = false;
    this.createError = null;
    this.isEditMode = false;
    this.editingInvoiceId = null;
    this.editingInvoiceNumber = '';
    this.clearCreatePendingImages();
  }

  /** Load full invoice then open the editor (from list row). */
  openEditFromList(row: InvoiceListItem): void {
    this.createError = null;
    this.invoiceService.getById(row.id).subscribe({
      next: (inv) => this.openEditInvoice(inv),
      error: (err: HttpErrorResponse) => {
        this.error = this.httpErrorMessage(err);
      }
    });
  }

  /** Open editor prefilled from an invoice (closes detail drawer if open). */
  openEditInvoice(inv: InvoiceDetail): void {
    this.createError = null;
    this.clearCreatePendingImages();
    this.isEditMode = true;
    this.editingInvoiceId = inv.id;
    this.editingInvoiceNumber = inv.invoiceNumber;
    this.populateFormFromInvoice(inv);
    this.createModalOpen = true;
    this.createUploadingImages = false;
    if (this.detailOpen) {
      this.detailOpen = false;
      this.selectedDetail = null;
      this.detailLoading = false;
      this.imageError = null;
      this.imageUploading = false;
    }
  }

  private toDateInputValue(iso: string): string {
    const s = (iso || '').trim();
    if (s.length >= 10) {
      return s.slice(0, 10);
    }
    return this.todayIsoDate();
  }

  private populateFormFromInvoice(inv: InvoiceDetail): void {
    while (this.lineItems.length) {
      this.lineItems.removeAt(0);
    }
    const sorted = [...inv.lineItems].sort((a, b) => a.sortOrder - b.sortOrder);
    for (const li of sorted) {
      this.lineItems.push(
        this.fb.group({
          serviceName: [li.serviceName, [Validators.maxLength(200)]],
          details: [li.details ?? ''],
          category: [li.category],
          unitType: [li.unitType ?? ''],
          unitPrice: [li.unitPrice, [Validators.required, Validators.min(0)]],
          quantity: [li.quantity, [Validators.required, Validators.min(0.01)]],
          discount: [li.discount, [Validators.min(0)]],
          isDiscountPercentage: [li.isDiscountPercentage],
          isTaxable: [li.isTaxable],
          sortOrder: [li.sortOrder]
        })
      );
    }
    if (this.lineItems.length === 0) {
      this.lineItems.push(this.newLineItemGroup());
    }
    this.createForm.patchValue({
      issuedDate: this.toDateInputValue(inv.issuedDate),
      dueDate: inv.dueDate ? this.toDateInputValue(inv.dueDate) : '',
      clientName: inv.clientName,
      clientPhone: inv.clientPhone ?? '',
      clientEmail: inv.clientEmail ?? '',
      clientAddress: inv.clientAddress ?? '',
      jobId: inv.jobId != null ? String(inv.jobId) : '',
      taxRate: inv.taxRate,
      status: inv.status,
      notes: inv.notes ?? ''
    });
  }

  private clearCreatePendingImages(): void {
    for (const row of this.createPendingImages) {
      URL.revokeObjectURL(row.objectUrl);
    }
    this.createPendingImages = [];
  }

  onCreateGalleryFilesSelected(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    // Snapshot into an array BEFORE clearing input.value — FileList is a live object
    // and gets wiped when value is reset.
    const files = Array.from(input.files ?? []);
    input.value = '';
    if (!files.length) {
      return;
    }
    this.createError = null;
    const allowed = new Set(['image/jpeg', 'image/jpg', 'image/png', 'image/webp', 'image/pjpeg']);
    const picked = files.filter((f) => allowed.has((f.type || '').toLowerCase()));
    if (!picked.length) {
      this.createError = 'Only JPEG, PNG, or WebP images are allowed.';
      return;
    }
    const room = this.maxInvoiceImages - this.createPendingImages.length;
    if (room <= 0) {
      this.createError = `You can add at most ${this.maxInvoiceImages} images per invoice.`;
      return;
    }
    const toAdd = picked.slice(0, room);
    if (picked.length > room) {
      this.createError = `Only the first ${room} file(s) were added (max ${this.maxInvoiceImages} per invoice).`;
    }
    for (const file of toAdd) {
      this.createPendingImageIdSeq += 1;
      this.createPendingImages.push({
        id: this.createPendingImageIdSeq,
        file,
        objectUrl: URL.createObjectURL(file)
      });
    }
  }

  removeCreatePendingImage(index: number): void {
    const row = this.createPendingImages[index];
    if (!row) {
      return;
    }
    URL.revokeObjectURL(row.objectUrl);
    this.createPendingImages.splice(index, 1);
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

  /** Create or update depending on {@link isEditMode}. */
  submitInvoiceForm(): void {
    this.createError = null;
    this.createForm.markAllAsTouched();
    if (this.createForm.invalid) {
      this.createError = 'Please fix the highlighted fields.';
      return;
    }

    const payload = this.buildPayloadFromForm();
    if (!payload) {
      return;
    }

    if (this.isEditMode && this.editingInvoiceId != null) {
      this.createSubmitting = true;
      const id = this.editingInvoiceId;
      this.invoiceService.update(id, payload).subscribe({
        next: (inv) => {
          this.createSubmitting = false;
          this.closeCreateModal();
          this.successMessage = `Invoice ${inv.invoiceNumber} updated.`;
          setTimeout(() => (this.successMessage = null), 4000);
          this.load();
          this.selectedDetail = inv;
          this.detailOpen = true;
        },
        error: (err: HttpErrorResponse) => {
          this.createSubmitting = false;
          this.createError = this.createFormHttpError(err);
        }
      });
      return;
    }

    this.createSubmitting = true;
    this.createUploadingImages = false;
    const filesToUpload = this.createPendingImages.map((r) => r.file);
    this.invoiceService.create(payload).subscribe({
      next: (inv) => {
        this.clearCreatePendingImages();
        if (filesToUpload.length === 0) {
          this.finishCreateSuccess(inv, null);
          return;
        }
        this.createUploadingImages = true;
        this.uploadImagesAfterCreate(inv.id, filesToUpload, 0, inv);
      },
      error: (err: HttpErrorResponse) => {
        this.createSubmitting = false;
        this.createUploadingImages = false;
        this.createError = this.createFormHttpError(err);
      }
    });
  }

  private buildPayloadFromForm(): CreateInvoicePayload | null {
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
      return null;
    }

    const invalidQty = lineItems.some((li) => li.quantity <= 0 || li.unitPrice < 0);
    if (invalidQty) {
      this.createError = 'Each line item needs quantity greater than 0 and a non-negative unit price.';
      return null;
    }

    const jobRaw = String(raw.jobId ?? '').trim();
    const jobIdParsed = jobRaw === '' ? null : Number(jobRaw);
    const jobIdPayload =
      jobIdParsed != null && Number.isFinite(jobIdParsed) && jobIdParsed > 0 ? jobIdParsed : null;

    const dueStr = String(raw.dueDate ?? '').trim();
    const issuedStr = String(raw.issuedDate ?? '').trim();

    return {
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
  }

  private uploadImagesAfterCreate(
    invoiceId: number,
    files: File[],
    index: number,
    lastGood: InvoiceDetail
  ): void {
    if (index >= files.length) {
      this.finishCreateSuccess(lastGood, null);
      return;
    }
    this.invoiceService.uploadImage(invoiceId, files[index]).subscribe({
      next: (updated) => {
        this.uploadImagesAfterCreate(invoiceId, files, index + 1, updated);
      },
      error: (err: HttpErrorResponse) => {
        const msg = this.httpErrorMessage(err);
        this.finishCreateSuccess(
          lastGood,
          `Invoice ${lastGood.invoiceNumber} was saved, but not all images uploaded (${msg}). Open the invoice to add or retry.`
        );
      }
    });
  }

  private finishCreateSuccess(inv: InvoiceDetail, imageWarning: string | null): void {
    this.createSubmitting = false;
    this.createUploadingImages = false;
    this.closeCreateModal();
    this.successMessage = imageWarning
      ? `Invoice ${inv.invoiceNumber} created. ${imageWarning}`
      : `Invoice ${inv.invoiceNumber} created.`;
    setTimeout(() => (this.successMessage = null), imageWarning ? 8000 : 5000);
    this.page = 1;
    this.load();
    this.showCreatedDetail(inv);
  }

  private showCreatedDetail(inv: InvoiceDetail): void {
    this.detailOpen = true;
    this.detailLoading = false;
    this.selectedDetail = inv;
  }

  resolveInvoiceImageUrl(path: string): string {
    return resolvePublicAssetUrl(path);
  }

  openDetail(row: InvoiceListItem): void {
    this.detailOpen = true;
    this.detailLoading = true;
    this.selectedDetail = null;
    this.imageError = null;
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
    this.imageError = null;
    this.imageUploading = false;
  }

  onGalleryFilesSelected(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    // Snapshot before clearing — FileList is live and gets wiped with input.value = ''.
    const files = Array.from(input.files ?? []);
    input.value = '';
    if (!files.length || !this.selectedDetail) {
      return;
    }
    this.imageError = null;
    const list = files.filter((f) => f.type.startsWith('image/'));
    if (!list.length) {
      this.imageError = 'Choose JPEG, PNG, or WebP images.';
      return;
    }
    this.uploadGalleryFilesSequential(this.selectedDetail.id, list, 0);
  }

  private uploadGalleryFilesSequential(invoiceId: number, files: File[], index: number): void {
    if (index >= files.length) {
      this.imageUploading = false;
      return;
    }
    const count = this.selectedDetail?.images?.length ?? 0;
    if (count >= this.maxInvoiceImages) {
      this.imageError = `Maximum ${this.maxInvoiceImages} images per invoice.`;
      this.imageUploading = false;
      return;
    }

    this.imageUploading = true;
    this.invoiceService.uploadImage(invoiceId, files[index]).subscribe({
      next: (inv) => {
        this.selectedDetail = inv;
        this.uploadGalleryFilesSequential(invoiceId, files, index + 1);
      },
      error: (err: HttpErrorResponse) => {
        this.imageUploading = false;
        this.imageError = this.httpErrorMessage(err);
      }
    });
  }

  removeInvoiceImage(img: InvoiceImage): void {
    if (!this.selectedDetail) {
      return;
    }
    const ok = confirm('Remove this photo from the invoice?');
    if (!ok) {
      return;
    }
    this.imageError = null;
    this.invoiceService.deleteImage(this.selectedDetail.id, img.id).subscribe({
      next: () => {
        this.invoiceService.getById(this.selectedDetail!.id).subscribe({
          next: (inv) => (this.selectedDetail = inv),
          error: (err: HttpErrorResponse) => (this.imageError = this.httpErrorMessage(err))
        });
      },
      error: (err: HttpErrorResponse) => (this.imageError = this.httpErrorMessage(err))
    });
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
