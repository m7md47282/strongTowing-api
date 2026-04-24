import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs/operators';
import { AccountsService } from '../../../../services/accounts.service';
import { ServicePricingService } from '../../../../services/service-pricing.service';
import {
  CreateInsuranceAccountPayload,
  InsuranceAccount
} from '../../../../models/insurance-account.model';
import {
  InsuranceAccountServiceRate,
  UpsertInsuranceAccountServiceRatePayload
} from '../../../../models/insurance-account-service-rate.model';
import { ServicePricingProfile } from '../../../../models/service-pricing.model';

@Component({
  selector: 'app-accounts',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './accounts.component.html',
  styleUrl: './accounts.component.scss'
})
export class AccountsComponent implements OnInit {
  accounts: InsuranceAccount[] = [];
  filteredAccounts: InsuranceAccount[] = [];
  loading = false;
  error: string | null = null;
  successMessage: string | null = null;

  includeInactive = false;
  searchTerm = '';

  showModal = false;
  editingId: number | null = null;
  submitting = false;
  accountForm: FormGroup;

  deleteConfirmId: number | null = null;

  showServiceRatesModal = false;
  serviceRatesAccount: InsuranceAccount | null = null;
  serviceRates: InsuranceAccountServiceRate[] = [];
  serviceRatesLoading = false;
  serviceCatalog: ServicePricingProfile[] = [];
  serviceRateSubmitting = false;
  editingRateId: number | null = null;
  serviceRateForm: FormGroup;

  constructor(
    private accountsService: AccountsService,
    private servicePricingService: ServicePricingService,
    private fb: FormBuilder
  ) {
    this.serviceRateForm = this.fb.group({
      servicePricingProfileId: [null as number | null, [Validators.required]],
      basePrice: [0, [Validators.min(0)]],
      pricePerMile: [0, [Validators.min(0)]]
    });

    this.accountForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      accountNumber: [''],
      contactName: [''],
      billingEmail: ['', [Validators.email]],
      billingPhone: [''],
      addressLine1: [''],
      addressLine2: [''],
      city: [''],
      state: [''],
      postalCode: [''],
      notes: [''],
      isActive: [true],
      hookupFee: [0, [Validators.min(0)]],
      rateAB: [0, [Validators.min(0)]],
      rateBC: [0, [Validators.min(0)]],
      rateCA: [0, [Validators.min(0)]]
    });
  }

  ngOnInit(): void {
    this.loadAccounts();
  }

  loadAccounts(): void {
    this.loading = true;
    this.error = null;
    this.accountsService
      .getAll(this.includeInactive)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (rows) => {
          this.accounts = rows;
          this.applyFilter();
        },
        error: (err: HttpErrorResponse) => {
          this.error = this.httpErrorMessage(err);
        }
      });
  }

  onIncludeInactiveChange(): void {
    this.loadAccounts();
  }

  onSearchChange(): void {
    this.applyFilter();
  }

  private applyFilter(): void {
    const q = this.searchTerm.trim().toLowerCase();
    if (!q) {
      this.filteredAccounts = [...this.accounts];
      return;
    }
    this.filteredAccounts = this.accounts.filter(
      (a) =>
        a.name.toLowerCase().includes(q) ||
        (a.accountNumber?.toLowerCase().includes(q) ?? false) ||
        (a.contactName?.toLowerCase().includes(q) ?? false) ||
        (a.billingEmail?.toLowerCase().includes(q) ?? false)
    );
  }

  openCreateModal(): void {
    this.editingId = null;
    this.accountForm.reset({
      name: '',
      accountNumber: '',
      contactName: '',
      billingEmail: '',
      billingPhone: '',
      addressLine1: '',
      addressLine2: '',
      city: '',
      state: '',
      postalCode: '',
      notes: '',
      isActive: true,
      hookupFee: 0,
      rateAB: 0,
      rateBC: 0,
      rateCA: 0
    });
    this.showModal = true;
    this.error = null;
    this.successMessage = null;
  }

  openEditModal(account: InsuranceAccount): void {
    this.editingId = account.id;
    this.accountForm.patchValue({
      name: account.name,
      accountNumber: account.accountNumber ?? '',
      contactName: account.contactName ?? '',
      billingEmail: account.billingEmail ?? '',
      billingPhone: account.billingPhone ?? '',
      addressLine1: account.addressLine1 ?? '',
      addressLine2: account.addressLine2 ?? '',
      city: account.city ?? '',
      state: account.state ?? '',
      postalCode: account.postalCode ?? '',
      notes: account.notes ?? '',
      isActive: account.isActive,
      hookupFee: account.hookupFee ?? 0,
      rateAB: account.rateAB ?? 0,
      rateBC: account.rateBC ?? 0,
      rateCA: account.rateCA ?? 0
    });
    this.showModal = true;
    this.error = null;
    this.successMessage = null;
  }

  closeModal(): void {
    this.showModal = false;
    this.editingId = null;
  }

  saveAccount(): void {
    if (this.accountForm.invalid) {
      this.accountForm.markAllAsTouched();
      return;
    }

    const raw = this.accountForm.getRawValue();
    const payload: CreateInsuranceAccountPayload = {
      name: (raw.name as string).trim(),
      accountNumber: this.emptyToNull(raw.accountNumber as string),
      contactName: this.emptyToNull(raw.contactName as string),
      billingEmail: this.emptyToNull(raw.billingEmail as string),
      billingPhone: this.emptyToNull(raw.billingPhone as string),
      addressLine1: this.emptyToNull(raw.addressLine1 as string),
      addressLine2: this.emptyToNull(raw.addressLine2 as string),
      city: this.emptyToNull(raw.city as string),
      state: this.emptyToNull(raw.state as string),
      postalCode: this.emptyToNull(raw.postalCode as string),
      notes: this.emptyToNull(raw.notes as string),
      isActive: Boolean(raw.isActive),
      hookupFee: Number(raw.hookupFee ?? 0),
      rateAB: Number(raw.rateAB ?? 0),
      rateBC: Number(raw.rateBC ?? 0),
      rateCA: Number(raw.rateCA ?? 0)
    };

    this.submitting = true;
    this.error = null;

    const req =
      this.editingId == null
        ? this.accountsService.create(payload)
        : this.accountsService.update(this.editingId, payload);

    req.pipe(finalize(() => (this.submitting = false))).subscribe({
      next: () => {
        this.successMessage =
          this.editingId == null ? 'Account created successfully.' : 'Account updated successfully.';
        this.closeModal();
        this.loadAccounts();
        setTimeout(() => (this.successMessage = null), 4000);
      },
      error: (err: HttpErrorResponse) => {
        this.error = this.httpErrorMessage(err);
      }
    });
  }

  confirmDelete(account: InsuranceAccount): void {
    this.deleteConfirmId = account.id;
  }

  cancelDelete(): void {
    this.deleteConfirmId = null;
  }

  executeDelete(): void {
    if (this.deleteConfirmId == null) return;
    const id = this.deleteConfirmId;
    this.submitting = true;
    this.error = null;
    this.accountsService
      .delete(id)
      .pipe(finalize(() => (this.submitting = false)))
      .subscribe({
        next: () => {
          this.deleteConfirmId = null;
          this.successMessage = 'Account deleted.';
          this.loadAccounts();
          setTimeout(() => (this.successMessage = null), 4000);
        },
        error: (err: HttpErrorResponse) => {
          this.error = this.httpErrorMessage(err);
        }
      });
  }

  formatDate(dateString: string | null): string {
    if (!dateString) return '—';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  private emptyToNull(s: string): string | null {
    const t = s?.trim();
    return t ? t : null;
  }

  private httpErrorMessage(err: HttpErrorResponse): string {
    const e = err.error;
    if (e && typeof e === 'object' && 'message' in e && typeof (e as { message?: string }).message === 'string') {
      return (e as { message: string }).message;
    }
    if (typeof e === 'string' && e.length > 0) return e;
    return err.message || 'Request failed.';
  }

  openServiceRatesModal(account: InsuranceAccount): void {
    this.serviceRatesAccount = account;
    this.showServiceRatesModal = true;
    this.editingRateId = null;
    this.error = null;
    this.serviceRates = [];
    this.serviceRateForm.reset({
      servicePricingProfileId: null,
      basePrice: 0,
      pricePerMile: 0
    });
    this.loadServiceCatalog();
    this.loadServiceRates(account.id);
  }

  closeServiceRatesModal(): void {
    this.showServiceRatesModal = false;
    this.serviceRatesAccount = null;
    this.editingRateId = null;
  }

  private loadServiceCatalog(): void {
    this.servicePricingService.getAll(true).subscribe({
      next: (rows) => (this.serviceCatalog = rows),
      error: () => (this.serviceCatalog = [])
    });
  }

  private loadServiceRates(accountId: number): void {
    this.serviceRatesLoading = true;
    this.accountsService
      .listServiceRates(accountId)
      .pipe(finalize(() => (this.serviceRatesLoading = false)))
      .subscribe({
        next: (rows) => (this.serviceRates = rows),
        error: (err: HttpErrorResponse) => {
          this.error = this.httpErrorMessage(err);
        }
      });
  }

  startEditServiceRate(row: InsuranceAccountServiceRate): void {
    this.editingRateId = row.id;
    this.serviceRateForm.patchValue({
      servicePricingProfileId: row.servicePricingProfileId,
      basePrice: row.basePrice,
      pricePerMile: row.pricePerMile
    });
  }

  cancelEditServiceRate(): void {
    this.editingRateId = null;
    this.serviceRateForm.reset({
      servicePricingProfileId: null,
      basePrice: 0,
      pricePerMile: 0
    });
  }

  saveServiceRate(): void {
    if (!this.serviceRatesAccount || this.serviceRateForm.invalid) {
      this.serviceRateForm.markAllAsTouched();
      return;
    }

    const raw = this.serviceRateForm.getRawValue();
    const payload: UpsertInsuranceAccountServiceRatePayload = {
      servicePricingProfileId: Number(raw.servicePricingProfileId),
      basePrice: Number(raw.basePrice ?? 0),
      pricePerMile: Number(raw.pricePerMile ?? 0)
    };

    this.serviceRateSubmitting = true;
    this.error = null;
    const accountId = this.serviceRatesAccount.id;

    const req =
      this.editingRateId == null
        ? this.accountsService.createServiceRate(accountId, payload)
        : this.accountsService.updateServiceRate(accountId, this.editingRateId, payload);

    req.pipe(finalize(() => (this.serviceRateSubmitting = false))).subscribe({
      next: () => {
        this.successMessage = 'Service pricing saved.';
        this.cancelEditServiceRate();
        this.loadServiceRates(accountId);
        setTimeout(() => (this.successMessage = null), 3000);
      },
      error: (err: HttpErrorResponse) => {
        this.error = this.httpErrorMessage(err);
      }
    });
  }

  deleteServiceRate(row: InsuranceAccountServiceRate): void {
    if (!this.serviceRatesAccount || !confirm(`Remove ${row.serviceName} pricing for this account?`)) {
      return;
    }
    this.serviceRateSubmitting = true;
    this.accountsService
      .deleteServiceRate(this.serviceRatesAccount.id, row.id)
      .pipe(finalize(() => (this.serviceRateSubmitting = false)))
      .subscribe({
        next: () => {
          this.successMessage = 'Removed.';
          this.loadServiceRates(this.serviceRatesAccount!.id);
          setTimeout(() => (this.successMessage = null), 3000);
        },
        error: (err: HttpErrorResponse) => {
          this.error = this.httpErrorMessage(err);
        }
      });
  }

  availableServiceProfilesForAdd(): ServicePricingProfile[] {
    const used = new Set(this.serviceRates.map((r) => r.servicePricingProfileId));
    if (this.editingRateId != null) {
      const cur = this.serviceRates.find((r) => r.id === this.editingRateId);
      if (cur) {
        used.delete(cur.servicePricingProfileId);
      }
    }
    return this.serviceCatalog.filter((p) => !used.has(p.id));
  }
}
