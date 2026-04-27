import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs/operators';
import { AccountsService } from '../../../../services/accounts.service';
import { AuthService } from '../../../../services/auth.service';
import { ServicePricingService } from '../../../../services/service-pricing.service';
import { InsuranceAccountServiceRate, UpsertInsuranceAccountServiceRatePayload } from '../../../../models/insurance-account-service-rate.model';
import { ServicePricingProfile } from '../../../../models/service-pricing.model';

@Component({
  selector: 'app-account-cash-call-rates',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './account-cash-call-rates.component.html',
  styleUrl: './account-cash-call-rates.component.scss'
})
export class AccountCashCallRatesComponent implements OnInit {
  accountId = 0;
  accountName = '';
  canEdit = false;

  rates: InsuranceAccountServiceRate[] = [];
  serviceCatalog: ServicePricingProfile[] = [];
  ratesLoading = false;
  submitting = false;
  error: string | null = null;
  successMessage: string | null = null;

  editingRateId: number | null = null;
  showEditorModal = false;
  deleteConfirmRow: InsuranceAccountServiceRate | null = null;
  form: FormGroup;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly fb: FormBuilder,
    private readonly authService: AuthService,
    private readonly accountsService: AccountsService,
    private readonly servicePricingService: ServicePricingService
  ) {
    this.form = this.fb.group({
      servicePricingProfileId: [null as number | null, [Validators.required]],
      basePrice: [0, [Validators.required, Validators.min(0)]],
      pricePerMile: [0, [Validators.required, Validators.min(0)]]
    });
  }

  ngOnInit(): void {
    const rawId = Number(this.route.snapshot.paramMap.get('accountId'));
    if (!Number.isFinite(rawId) || rawId <= 0) {
      this.error = 'Invalid account id.';
      return;
    }

    this.accountId = rawId;
    this.canEdit = this.authService.isAdmin() || this.authService.isSuperAdmin();

    const queryName = (this.route.snapshot.queryParamMap.get('accountName') ?? '').trim();
    if (queryName) {
      this.accountName = queryName;
    } else {
      this.accountName = `Account #${this.accountId}`;
    }

    if (this.canEdit) {
      this.loadServiceCatalog();
    }
    this.loadRates();
  }

  loadRates(): void {
    this.ratesLoading = true;
    this.error = null;
    this.accountsService
      .listServiceRates(this.accountId)
      .pipe(finalize(() => (this.ratesLoading = false)))
      .subscribe({
        next: (rows) => {
          this.rates = rows;
        },
        error: (err: HttpErrorResponse) => {
          this.error = this.httpErrorMessage(err);
        }
      });
  }

  private loadServiceCatalog(): void {
    this.servicePricingService.getAll(true).subscribe({
      next: (rows) => {
        this.serviceCatalog = rows;
      },
      error: () => {
        this.serviceCatalog = [];
      }
    });
  }

  goBack(): void {
    if (this.router.url.startsWith('/dispatcher')) {
      void this.router.navigate(['/dispatcher/jobs']);
      return;
    }
    void this.router.navigate(['/admin/accounts']);
  }

  startEdit(row: InsuranceAccountServiceRate): void {
    if (!this.canEdit) return;
    this.editingRateId = row.id;
    this.showEditorModal = true;
    this.error = null;
    this.form.patchValue({
      servicePricingProfileId: row.servicePricingProfileId,
      basePrice: row.basePrice,
      pricePerMile: row.pricePerMile
    });
  }

  openAddModal(): void {
    if (!this.canEdit) return;
    this.cancelEdit();
    this.showEditorModal = true;
    this.error = null;
  }

  cancelEdit(): void {
    this.editingRateId = null;
    this.showEditorModal = false;
    this.form.reset({
      servicePricingProfileId: null,
      basePrice: 0,
      pricePerMile: 0
    });
  }

  save(): void {
    if (!this.canEdit) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const payload: UpsertInsuranceAccountServiceRatePayload = {
      servicePricingProfileId: Number(raw.servicePricingProfileId),
      basePrice: Number(raw.basePrice ?? 0),
      pricePerMile: Number(raw.pricePerMile ?? 0)
    };

    this.submitting = true;
    this.error = null;

    const req = this.editingRateId == null
      ? this.accountsService.createServiceRate(this.accountId, payload)
      : this.accountsService.updateServiceRate(this.accountId, this.editingRateId, payload);

    req.pipe(finalize(() => (this.submitting = false))).subscribe({
      next: () => {
        this.successMessage = 'Cash call rate saved.';
        this.cancelEdit();
        this.loadRates();
        setTimeout(() => (this.successMessage = null), 3000);
      },
      error: (err: HttpErrorResponse) => {
        this.error = this.httpErrorMessage(err);
      }
    });
  }

  remove(row: InsuranceAccountServiceRate): void {
    if (!this.canEdit) return;
    this.deleteConfirmRow = row;
  }

  cancelDelete(): void {
    this.deleteConfirmRow = null;
  }

  confirmDelete(): void {
    if (!this.canEdit || !this.deleteConfirmRow) return;
    const row = this.deleteConfirmRow;

    this.submitting = true;
    this.error = null;
    this.accountsService
      .deleteServiceRate(this.accountId, row.id)
      .pipe(finalize(() => (this.submitting = false)))
      .subscribe({
        next: () => {
          this.deleteConfirmRow = null;
          this.successMessage = 'Cash call rate removed.';
          this.loadRates();
          setTimeout(() => (this.successMessage = null), 3000);
        },
        error: (err: HttpErrorResponse) => {
          this.error = this.httpErrorMessage(err);
        }
      });
  }

  availableServiceProfilesForAdd(): ServicePricingProfile[] {
    const used = new Set(this.rates.map((r) => r.servicePricingProfileId));
    if (this.editingRateId != null) {
      const current = this.rates.find((r) => r.id === this.editingRateId);
      if (current) {
        used.delete(current.servicePricingProfileId);
      }
    }
    return this.serviceCatalog.filter((p) => !used.has(p.id));
  }

  private httpErrorMessage(err: HttpErrorResponse): string {
    const e = err.error;
    if (e && typeof e === 'object' && 'message' in e && typeof (e as { message?: string }).message === 'string') {
      return (e as { message: string }).message;
    }
    if (typeof e === 'string' && e.length > 0) return e;
    return err.message || 'Request failed.';
  }
}
