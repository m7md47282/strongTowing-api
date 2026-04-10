import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { interval, Subscription } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { RoleId } from '../../../../constants/user-roles.constants';
import { AuthService } from '../../../../services/auth.service';
import { SettingsService, SystemSettings, UpdateSystemSettingsRequest } from '../../../../services/settings.service';
import {
  CreatePaymentIntentResponse,
  CreateStripePaymentLinkResponse,
  PaymentService
} from '../../../../services/payment.service';
import { LocationPickerComponent } from '../../../shared/location-picker/location-picker.component';
import {
  VehicleCatalogAdminService,
  VehicleCatalogSyncStatus
} from '../../../../services/vehicle-catalog-admin.service';

type SettingsTab = 'general' | 'payments' | 'vehicleCatalog';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LocationPickerComponent],
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss']
})
export class SettingsComponent implements OnInit, OnDestroy {
  activeTab: SettingsTab = 'general';

  /** NHTSA catalog sync (Admin / Super Admin) */
  catalogSyncStatus: VehicleCatalogSyncStatus | null = null;
  catalogSyncSubmitting = false;
  catalogSyncError: string | null = null;
  private catalogPollSub?: Subscription;
  settingsForm: FormGroup;
  generalForm: FormGroup;
  paymentTestForm: FormGroup;
  loading = false;
  saving = false;
  savingOffice = false;
  error: string | null = null;
  successMessage: string | null = null;
  currentSettings: SystemSettings | null = null;
  showPaymentTestModal = false;
  paymentTestLoading = false;
  paymentTestError: string | null = null;
  paymentIntentResult: CreatePaymentIntentResponse | null = null;
  paymentLinkResult: CreateStripePaymentLinkResponse | null = null;

  constructor(
    private fb: FormBuilder,
    private settingsService: SettingsService,
    private authService: AuthService,
    private paymentService: PaymentService,
    private vehicleCatalogAdmin: VehicleCatalogAdminService
  ) {
    this.generalForm = this.fb.group({
      officeLatitude: [null as number | null],
      officeLongitude: [null as number | null]
    });

    this.settingsForm = this.fb.group({
      driverCommissionPercentage: [30, [Validators.required, Validators.min(0), Validators.max(100)]],
      stripeEnabled: [false],
      stripeMode: ['test', [Validators.required]],
      stripePublicKey: [''],
      stripeSecretKey: [''],
      stripeWebhookSecret: [''],
      stripeTestPublicKey: [''],
      stripeTestSecretKey: [''],
      stripeTestWebhookSecret: [''],
      stripeLivePublicKey: [''],
      stripeLiveSecretKey: [''],
      stripeLiveWebhookSecret: [''],
      defaultPricingTaxPercent: [10, [Validators.required, Validators.min(0), Validators.max(100)]],
      defaultPricingServiceChargePercent: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
      defaultPricingHookupFee: [75, [Validators.required, Validators.min(0)]],
      maxDiscountPercent: [100, [Validators.required, Validators.min(0), Validators.max(100)]],
      allowManualTotalOverride: [false],
      manualOverrideRequiresReason: [true],
      pricingMismatchTolerance: [1, [Validators.required, Validators.min(0)]],
      pricingRoundingMode: ['AwayFromZero', [Validators.required]]
    });

    this.paymentTestForm = this.fb.group({
      jobId: [1, [Validators.required, Validators.min(1)]],
      amount: [25, [Validators.required, Validators.min(0.5)]],
      currency: ['usd', [Validators.required]],
      successUrl: ['']
    });
  }

  ngOnInit(): void {
    this.loadSettings();
  }

  ngOnDestroy(): void {
    this.catalogPollSub?.unsubscribe();
  }

  isAdminOrSuperAdmin(): boolean {
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser) {
      return false;
    }
    const roleId =
      typeof currentUser.roleId === 'string' ? parseInt(currentUser.roleId, 10) : Number(currentUser.roleId);
    return roleId === RoleId.SuperAdmin || roleId === RoleId.Admin;
  }

  isSuperAdmin(): boolean {
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser) {
      return false;
    }

    const roleId = typeof currentUser.roleId === 'string'
      ? parseInt(currentUser.roleId, 10)
      : Number(currentUser.roleId);

    return roleId === RoleId.SuperAdmin;
  }

  selectTab(tab: SettingsTab): void {
    this.activeTab = tab;
    this.error = null;
    this.successMessage = null;
    this.catalogSyncError = null;
    if (tab === 'vehicleCatalog' && this.isAdminOrSuperAdmin()) {
      this.refreshCatalogSyncStatus();
    }
  }

  refreshCatalogSyncStatus(): void {
    this.catalogSyncError = null;
    this.vehicleCatalogAdmin.getSyncStatus().subscribe({
      next: (s) => {
        this.catalogSyncStatus = s;
        if (s.status === 1) {
          this.ensureCatalogPolling();
        }
      },
      error: () => {
        this.catalogSyncError = 'Could not load vehicle catalog sync status.';
      }
    });
  }

  private ensureCatalogPolling(): void {
    if (this.catalogPollSub && !this.catalogPollSub.closed) {
      return;
    }
    this.catalogPollSub?.unsubscribe();
    this.catalogPollSub = interval(3000)
      .pipe(switchMap(() => this.vehicleCatalogAdmin.getSyncStatus()))
      .subscribe({
        next: (s) => {
          this.catalogSyncStatus = s;
          if (s.status !== 1) {
            this.catalogPollSub?.unsubscribe();
            this.catalogPollSub = undefined;
          }
        },
        error: () => {
          this.catalogPollSub?.unsubscribe();
          this.catalogPollSub = undefined;
        }
      });
  }

  triggerVehicleCatalogSync(): void {
    if (!this.isAdminOrSuperAdmin()) {
      return;
    }
    this.catalogSyncSubmitting = true;
    this.catalogSyncError = null;
    this.vehicleCatalogAdmin
      .postSync()
      .pipe(finalize(() => (this.catalogSyncSubmitting = false)))
      .subscribe({
        next: () => {
          this.refreshCatalogSyncStatus();
          this.ensureCatalogPolling();
        },
        error: (err) => {
          this.catalogSyncError =
            err.error?.message || err.error?.error || 'Could not start sync. It may already be running.';
        }
      });
  }

  catalogStatusLabel(status: number): string {
    switch (status) {
      case 0:
        return 'Idle';
      case 1:
        return 'Running';
      case 2:
        return 'Succeeded';
      case 3:
        return 'Failed';
      default:
        return 'Unknown';
    }
  }

  /** 0–100 for makes loop progress while sync is running. */
  catalogSyncProgressPercent(): number {
    const s = this.catalogSyncStatus;
    const total = s?.totalMakes ?? 0;
    const done = s?.makesProcessed ?? 0;
    if (!s || total <= 0) {
      return 0;
    }
    return Math.min(100, Math.round((100 * done) / total));
  }

  setStripeMode(mode: 'test' | 'live'): void {
    this.settingsForm.patchValue({ stripeMode: mode });
  }

  isStripeMode(mode: 'test' | 'live'): boolean {
    return this.settingsForm.get('stripeMode')?.value === mode;
  }

  onOfficePositionChange(pos: { lat: number; lng: number }): void {
    this.generalForm.patchValue({
      officeLatitude: pos.lat,
      officeLongitude: pos.lng
    });
  }

  clearOfficeLocation(): void {
    this.generalForm.patchValue({ officeLatitude: null, officeLongitude: null });
  }

  private buildUpdateFromCurrent(): UpdateSystemSettingsRequest {
    const c = this.currentSettings!;
    return {
      driverCommissionPercentage: c.driverCommissionPercentage,
      stripePublicKey: c.stripePublicKey ?? null,
      stripeSecretKey: null,
      stripeWebhookSecret: null,
      stripeEnabled: c.stripeEnabled,
      stripeTestPublicKey: c.stripeTestPublicKey ?? null,
      stripeTestSecretKey: null,
      stripeTestWebhookSecret: null,
      stripeLivePublicKey: c.stripeLivePublicKey ?? null,
      stripeLiveSecretKey: null,
      stripeLiveWebhookSecret: null,
      stripeMode: c.stripeMode,
      preAuthorizationEnabled: c.preAuthorizationEnabled,
      preAuthorizationMinAmount: c.preAuthorizationMinAmount,
      preAuthorizationMaxAmount: c.preAuthorizationMaxAmount,
      fraudReviewScoreThreshold: c.fraudReviewScoreThreshold,
      duplicateRequestWindowMinutes: c.duplicateRequestWindowMinutes,
      cancelFeeBeforeDispatchPercent: c.cancelFeeBeforeDispatchPercent,
      cancelFeeAfterDispatchPercent: c.cancelFeeAfterDispatchPercent,
      cancelFeeAfterArrivalPercent: c.cancelFeeAfterArrivalPercent,
      defaultPricingTaxPercent: c.defaultPricingTaxPercent,
      defaultPricingServiceChargePercent: c.defaultPricingServiceChargePercent,
      defaultPricingHookupFee: c.defaultPricingHookupFee,
      maxDiscountPercent: c.maxDiscountPercent,
      allowManualTotalOverride: c.allowManualTotalOverride,
      manualOverrideRequiresReason: c.manualOverrideRequiresReason,
      pricingMismatchTolerance: c.pricingMismatchTolerance,
      pricingRoundingMode: c.pricingRoundingMode,
      officeLatitude: c.officeLatitude ?? null,
      officeLongitude: c.officeLongitude ?? null
    };
  }

  loadSettings(): void {
    this.loading = true;
    this.error = null;

    this.settingsService.getSettings()
      .pipe(finalize(() => { this.loading = false; }))
      .subscribe({
        next: (settings) => {
          this.currentSettings = settings;
          this.settingsForm.patchValue({
            driverCommissionPercentage: settings.driverCommissionPercentage,
            stripeEnabled: settings.stripeEnabled,
            stripeMode: settings.stripeMode || 'test',
            stripePublicKey: settings.stripePublicKey || '',
            stripeTestPublicKey: settings.stripeTestPublicKey || '',
            stripeLivePublicKey: settings.stripeLivePublicKey || '',
            defaultPricingTaxPercent: settings.defaultPricingTaxPercent,
            defaultPricingServiceChargePercent: settings.defaultPricingServiceChargePercent,
            defaultPricingHookupFee: settings.defaultPricingHookupFee,
            maxDiscountPercent: settings.maxDiscountPercent,
            allowManualTotalOverride: settings.allowManualTotalOverride,
            manualOverrideRequiresReason: settings.manualOverrideRequiresReason,
            pricingMismatchTolerance: settings.pricingMismatchTolerance,
            pricingRoundingMode: settings.pricingRoundingMode || 'AwayFromZero'
          });
          this.generalForm.patchValue({
            officeLatitude: settings.officeLatitude ?? null,
            officeLongitude: settings.officeLongitude ?? null
          });
        },
        error: (err) => {
          this.error = err.error?.message || err.error?.error || 'Failed to load settings.';
        }
      });
  }

  saveOfficeLocation(): void {
    if (!this.isSuperAdmin()) {
      this.error = 'Only Super Admin can update settings.';
      return;
    }

    if (!this.currentSettings) {
      return;
    }

    const gv = this.generalForm.value as { officeLatitude: unknown; officeLongitude: unknown };
    const parseOpt = (v: unknown): number | null => {
      if (v === null || v === undefined || v === '') {
        return null;
      }
      const n = Number(v);
      return Number.isNaN(n) ? null : n;
    };
    const officeLatitude = parseOpt(gv.officeLatitude);
    const officeLongitude = parseOpt(gv.officeLongitude);
    if ((officeLatitude === null) !== (officeLongitude === null)) {
      this.error = 'Set both latitude and longitude, or clear both to use appsettings fallback.';
      return;
    }

    this.savingOffice = true;
    this.error = null;
    this.successMessage = null;

    if (
      officeLatitude != null &&
      officeLongitude != null &&
      (officeLatitude < -90 || officeLatitude > 90 || officeLongitude < -180 || officeLongitude > 180)
    ) {
      this.error = 'Invalid coordinates.';
      this.savingOffice = false;
      return;
    }

    const payload: UpdateSystemSettingsRequest = {
      ...this.buildUpdateFromCurrent(),
      officeLatitude,
      officeLongitude
    };

    this.settingsService.updateSettings(payload)
      .pipe(finalize(() => { this.savingOffice = false; }))
      .subscribe({
        next: (settings) => {
          this.currentSettings = settings;
          this.generalForm.patchValue({
            officeLatitude: settings.officeLatitude ?? null,
            officeLongitude: settings.officeLongitude ?? null
          });
          this.successMessage = 'Office location saved.';
        },
        error: (err) => {
          this.error = err.error?.message || err.error?.error || 'Failed to save office location.';
        }
      });
  }

  saveSettings(): void {
    if (!this.isSuperAdmin()) {
      this.error = 'Only Super Admin can update settings.';
      return;
    }

    if (!this.currentSettings) {
      return;
    }

    if (this.settingsForm.invalid) {
      this.settingsForm.markAllAsTouched();
      return;
    }

    this.saving = true;
    this.error = null;
    this.successMessage = null;

    const formValue = this.settingsForm.value;
    const payload: UpdateSystemSettingsRequest = {
      ...this.buildUpdateFromCurrent(),
      driverCommissionPercentage: Number(formValue.driverCommissionPercentage ?? 30),
      stripeEnabled: !!formValue.stripeEnabled,
      stripeMode: formValue.stripeMode === 'live' ? 'live' : 'test',
      stripePublicKey: formValue.stripePublicKey || null,
      stripeSecretKey: formValue.stripeSecretKey || null,
      stripeWebhookSecret: formValue.stripeWebhookSecret || null,
      stripeTestPublicKey: formValue.stripeTestPublicKey || null,
      stripeTestSecretKey: formValue.stripeTestSecretKey || null,
      stripeTestWebhookSecret: formValue.stripeTestWebhookSecret || null,
      stripeLivePublicKey: formValue.stripeLivePublicKey || null,
      stripeLiveSecretKey: formValue.stripeLiveSecretKey || null,
      stripeLiveWebhookSecret: formValue.stripeLiveWebhookSecret || null,
      defaultPricingTaxPercent: Number(formValue.defaultPricingTaxPercent ?? 10),
      defaultPricingServiceChargePercent: Number(formValue.defaultPricingServiceChargePercent ?? 0),
      defaultPricingHookupFee: Number(formValue.defaultPricingHookupFee ?? 75),
      maxDiscountPercent: Number(formValue.maxDiscountPercent ?? 100),
      allowManualTotalOverride: !!formValue.allowManualTotalOverride,
      manualOverrideRequiresReason: !!formValue.manualOverrideRequiresReason,
      pricingMismatchTolerance: Number(formValue.pricingMismatchTolerance ?? 1),
      pricingRoundingMode: formValue.pricingRoundingMode === 'ToEven' ? 'ToEven' : 'AwayFromZero'
    };

    this.settingsService.updateSettings(payload)
      .pipe(finalize(() => { this.saving = false; }))
      .subscribe({
        next: (settings) => {
          this.currentSettings = settings;
          this.generalForm.patchValue({
            officeLatitude: settings.officeLatitude ?? null,
            officeLongitude: settings.officeLongitude ?? null
          });
          this.successMessage = 'Settings saved successfully.';

          this.settingsForm.patchValue({
            stripeSecretKey: '',
            stripeWebhookSecret: '',
            stripeTestSecretKey: '',
            stripeTestWebhookSecret: '',
            stripeLiveSecretKey: '',
            stripeLiveWebhookSecret: ''
          });
        },
        error: (err) => {
          this.error = err.error?.message || err.error?.error || 'Failed to save settings.';
        }
      });
  }

  openPaymentTestModal(): void {
    this.showPaymentTestModal = true;
    this.paymentTestError = null;
    this.paymentIntentResult = null;
    this.paymentLinkResult = null;
  }

  closePaymentTestModal(): void {
    this.showPaymentTestModal = false;
    this.paymentTestLoading = false;
    this.paymentTestError = null;
    this.paymentIntentResult = null;
    this.paymentLinkResult = null;
  }

  createTestPaymentIntent(): void {
    if (this.paymentTestForm.invalid) {
      this.paymentTestForm.markAllAsTouched();
      return;
    }

    const formValue = this.paymentTestForm.value;
    this.paymentTestLoading = true;
    this.paymentTestError = null;
    this.paymentIntentResult = null;

    this.paymentService.createPaymentIntent({
      jobId: Number(formValue.jobId),
      amount: Number(formValue.amount),
      currency: String(formValue.currency || 'usd')
    })
    .pipe(finalize(() => { this.paymentTestLoading = false; }))
    .subscribe({
      next: (response) => {
        this.paymentIntentResult = response;
      },
      error: (err) => {
        this.paymentTestError = err.error?.message || err.error?.error || 'Failed to create payment intent.';
      }
    });
  }

  createTestPaymentLink(): void {
    if (this.paymentTestForm.invalid) {
      this.paymentTestForm.markAllAsTouched();
      return;
    }

    const formValue = this.paymentTestForm.value;
    this.paymentTestLoading = true;
    this.paymentTestError = null;
    this.paymentLinkResult = null;

    this.paymentService.createStripePaymentLink({
      jobId: Number(formValue.jobId),
      amount: Number(formValue.amount),
      successUrl: formValue.successUrl ? String(formValue.successUrl) : undefined
    })
    .pipe(finalize(() => { this.paymentTestLoading = false; }))
    .subscribe({
      next: (response) => {
        this.paymentLinkResult = response;
      },
      error: (err) => {
        this.paymentTestError = err.error?.message || err.error?.error || 'Failed to create payment link.';
      }
    });
  }
}
