import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { interval, Subscription } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { RoleId } from '../../../../constants/user-roles.constants';
import { AuthService } from '../../../../services/auth.service';
import {
  SettingsService,
  SystemSettings,
  TestEmailResponse,
  TestSmsResponse,
  UpdateSystemSettingsRequest
} from '../../../../services/settings.service';
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

type SettingsTab = 'general' | 'payments' | 'vehicleCatalog' | 'sms' | 'email';

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
  smsForm: FormGroup;
  emailForm: FormGroup;
  smsTestForm: FormGroup;
  emailTestForm: FormGroup;
  paymentTestForm: FormGroup;
  loading = false;
  saving = false;
  savingSms = false;
  savingEmail = false;
  savingOffice = false;
  error: string | null = null;
  successMessage: string | null = null;
  currentSettings: SystemSettings | null = null;
  showPaymentTestModal = false;
  paymentTestLoading = false;
  paymentTestError: string | null = null;
  paymentIntentResult: CreatePaymentIntentResponse | null = null;
  paymentLinkResult: CreateStripePaymentLinkResponse | null = null;

  showSmsTestModal = false;
  smsTestLoading = false;
  smsTestError: string | null = null;
  smsTestResult: TestSmsResponse | null = null;

  showEmailTestModal = false;
  emailTestLoading = false;
  emailTestError: string | null = null;
  emailTestResult: TestEmailResponse | null = null;

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

    this.smsTestForm = this.fb.group({
      toPhone: ['', Validators.required],
      message: ['']
    });

    this.emailTestForm = this.fb.group({
      toEmail: ['', Validators.required],
      subject: [''],
      htmlBody: ['']
    });

    this.smsForm = this.fb.group({
      smsEnabled: [true],
      smsTwilioAccountSid: [''],
      smsTwilioAuthToken: [''],
      smsTwilioFromNumber: [''],
      smsTwilioMessagingServiceSid: [''],
      smsDriverJobAssigned: [true],
      smsDriverJobCompleted: [true],
      smsDriverPayrollPaid: [true],
      smsClientJobCreated: [true],
      smsClientFraudUnderReview: [true],
      smsClientDriverAssigned: [true],
      smsClientStatusOnRoute: [true],
      smsClientStatusOnScene: [true],
      smsClientStatusLoaded: [true],
      smsClientPaymentLinkCreated: [true],
      smsClientPaymentSucceeded: [true],
      smsClientPaymentFailed: [true],
      smsClientJobCancelled: [true],
      smsClientJobCompleted: [true]
    });

    this.emailForm = this.fb.group({
      emailEnabled: [true],
      postmarkServerToken: [''],
      postmarkDefaultFromEmail: [''],
      postmarkMessageStream: [''],
      emailDriverJobAssigned: [true],
      emailDriverJobCompleted: [true],
      emailDriverPayrollPaid: [true],
      emailClientJobCreated: [true],
      emailClientFraudUnderReview: [true],
      emailClientDriverAssigned: [true],
      emailClientStatusOnRoute: [true],
      emailClientStatusOnScene: [true],
      emailClientStatusLoaded: [true],
      emailClientPaymentLinkCreated: [true],
      emailClientPaymentSucceeded: [true],
      emailClientPaymentFailed: [true],
      emailClientJobCancelled: [true],
      emailClientJobCompleted: [true]
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
      officeLongitude: c.officeLongitude ?? null,

      smsEnabled: c.smsEnabled ?? true,
      smsTwilioAccountSid: c.smsTwilioAccountSid ?? null,
      smsTwilioAuthToken: null,
      smsTwilioFromNumber: c.smsTwilioFromNumber ?? null,
      smsTwilioMessagingServiceSid: c.smsTwilioMessagingServiceSid ?? null,
      smsDriverJobAssigned: c.smsDriverJobAssigned ?? true,
      smsDriverJobCompleted: c.smsDriverJobCompleted ?? true,
      smsDriverPayrollPaid: c.smsDriverPayrollPaid ?? true,
      smsClientJobCreated: c.smsClientJobCreated ?? true,
      smsClientFraudUnderReview: c.smsClientFraudUnderReview ?? true,
      smsClientDriverAssigned: c.smsClientDriverAssigned ?? true,
      smsClientStatusOnRoute: c.smsClientStatusOnRoute ?? true,
      smsClientStatusOnScene: c.smsClientStatusOnScene ?? true,
      smsClientStatusLoaded: c.smsClientStatusLoaded ?? true,
      smsClientPaymentLinkCreated: c.smsClientPaymentLinkCreated ?? true,
      smsClientPaymentSucceeded: c.smsClientPaymentSucceeded ?? true,
      smsClientPaymentFailed: c.smsClientPaymentFailed ?? true,
      smsClientJobCancelled: c.smsClientJobCancelled ?? true,
      smsClientJobCompleted: c.smsClientJobCompleted ?? true,

      emailEnabled: c.emailEnabled ?? true,
      postmarkServerToken: null,
      postmarkDefaultFromEmail: c.postmarkDefaultFromEmail ?? null,
      postmarkMessageStream: c.postmarkMessageStream ?? null,
      emailDriverJobAssigned: c.emailDriverJobAssigned ?? true,
      emailDriverJobCompleted: c.emailDriverJobCompleted ?? true,
      emailDriverPayrollPaid: c.emailDriverPayrollPaid ?? true,
      emailClientJobCreated: c.emailClientJobCreated ?? true,
      emailClientFraudUnderReview: c.emailClientFraudUnderReview ?? true,
      emailClientDriverAssigned: c.emailClientDriverAssigned ?? true,
      emailClientStatusOnRoute: c.emailClientStatusOnRoute ?? true,
      emailClientStatusOnScene: c.emailClientStatusOnScene ?? true,
      emailClientStatusLoaded: c.emailClientStatusLoaded ?? true,
      emailClientPaymentLinkCreated: c.emailClientPaymentLinkCreated ?? true,
      emailClientPaymentSucceeded: c.emailClientPaymentSucceeded ?? true,
      emailClientPaymentFailed: c.emailClientPaymentFailed ?? true,
      emailClientJobCancelled: c.emailClientJobCancelled ?? true,
      emailClientJobCompleted: c.emailClientJobCompleted ?? true
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
          this.patchSmsFormFromSettings(settings);
          this.patchEmailFormFromSettings(settings);
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
          this.patchEmailFormFromSettings(settings);
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
          this.patchSmsFormFromSettings(settings);
          this.patchEmailFormFromSettings(settings);
        },
        error: (err) => {
          this.error = err.error?.message || err.error?.error || 'Failed to save settings.';
        }
      });
  }

  saveSmsSettings(): void {
    if (!this.isSuperAdmin()) {
      this.error = 'Only Super Admin can update settings.';
      return;
    }

    if (!this.currentSettings) {
      return;
    }

    this.savingSms = true;
    this.error = null;
    this.successMessage = null;

    const v = this.smsForm.getRawValue();
    const token =
      v.smsTwilioAuthToken && String(v.smsTwilioAuthToken).trim().length > 0
        ? String(v.smsTwilioAuthToken).trim()
        : null;

    const payload: UpdateSystemSettingsRequest = {
      ...this.buildUpdateFromCurrent(),
      smsEnabled: !!v.smsEnabled,
      smsTwilioAccountSid: v.smsTwilioAccountSid || null,
      smsTwilioAuthToken: token,
      smsTwilioFromNumber: v.smsTwilioFromNumber || null,
      smsTwilioMessagingServiceSid: v.smsTwilioMessagingServiceSid || null,
      smsDriverJobAssigned: !!v.smsDriverJobAssigned,
      smsDriverJobCompleted: !!v.smsDriverJobCompleted,
      smsDriverPayrollPaid: !!v.smsDriverPayrollPaid,
      smsClientJobCreated: !!v.smsClientJobCreated,
      smsClientFraudUnderReview: !!v.smsClientFraudUnderReview,
      smsClientDriverAssigned: !!v.smsClientDriverAssigned,
      smsClientStatusOnRoute: !!v.smsClientStatusOnRoute,
      smsClientStatusOnScene: !!v.smsClientStatusOnScene,
      smsClientStatusLoaded: !!v.smsClientStatusLoaded,
      smsClientPaymentLinkCreated: !!v.smsClientPaymentLinkCreated,
      smsClientPaymentSucceeded: !!v.smsClientPaymentSucceeded,
      smsClientPaymentFailed: !!v.smsClientPaymentFailed,
      smsClientJobCancelled: !!v.smsClientJobCancelled,
      smsClientJobCompleted: !!v.smsClientJobCompleted
    };

    this.settingsService
      .updateSettings(payload)
      .pipe(finalize(() => {
        this.savingSms = false;
      }))
      .subscribe({
        next: (settings) => {
          this.currentSettings = settings;
          this.patchSmsFormFromSettings(settings);
          this.patchEmailFormFromSettings(settings);
          this.successMessage = 'SMS settings saved.';
        },
        error: (err) => {
          this.error = err.error?.message || err.error?.error || 'Failed to save SMS settings.';
        }
      });
  }

  saveEmailSettings(): void {
    if (!this.isSuperAdmin()) {
      this.error = 'Only Super Admin can update settings.';
      return;
    }

    if (!this.currentSettings) {
      return;
    }

    this.savingEmail = true;
    this.error = null;
    this.successMessage = null;

    const v = this.emailForm.getRawValue();
    const token =
      v.postmarkServerToken && String(v.postmarkServerToken).trim().length > 0
        ? String(v.postmarkServerToken).trim()
        : null;

    const payload: UpdateSystemSettingsRequest = {
      ...this.buildUpdateFromCurrent(),
      emailEnabled: !!v.emailEnabled,
      postmarkServerToken: token,
      postmarkDefaultFromEmail: v.postmarkDefaultFromEmail || null,
      postmarkMessageStream: v.postmarkMessageStream || null,
      emailDriverJobAssigned: !!v.emailDriverJobAssigned,
      emailDriverJobCompleted: !!v.emailDriverJobCompleted,
      emailDriverPayrollPaid: !!v.emailDriverPayrollPaid,
      emailClientJobCreated: !!v.emailClientJobCreated,
      emailClientFraudUnderReview: !!v.emailClientFraudUnderReview,
      emailClientDriverAssigned: !!v.emailClientDriverAssigned,
      emailClientStatusOnRoute: !!v.emailClientStatusOnRoute,
      emailClientStatusOnScene: !!v.emailClientStatusOnScene,
      emailClientStatusLoaded: !!v.emailClientStatusLoaded,
      emailClientPaymentLinkCreated: !!v.emailClientPaymentLinkCreated,
      emailClientPaymentSucceeded: !!v.emailClientPaymentSucceeded,
      emailClientPaymentFailed: !!v.emailClientPaymentFailed,
      emailClientJobCancelled: !!v.emailClientJobCancelled,
      emailClientJobCompleted: !!v.emailClientJobCompleted
    };

    this.settingsService
      .updateSettings(payload)
      .pipe(
        finalize(() => {
          this.savingEmail = false;
        })
      )
      .subscribe({
        next: (settings) => {
          this.currentSettings = settings;
          this.patchSmsFormFromSettings(settings);
          this.patchEmailFormFromSettings(settings);
          this.successMessage = 'Email settings saved.';
        },
        error: (err) => {
          this.error = err.error?.message || err.error?.error || 'Failed to save email settings.';
        }
      });
  }

  private patchSmsFormFromSettings(settings: SystemSettings): void {
    this.smsForm.patchValue({
      smsEnabled: settings.smsEnabled ?? true,
      smsTwilioAccountSid: settings.smsTwilioAccountSid || '',
      smsTwilioAuthToken: '',
      smsTwilioFromNumber: settings.smsTwilioFromNumber || '',
      smsTwilioMessagingServiceSid: settings.smsTwilioMessagingServiceSid || '',
      smsDriverJobAssigned: settings.smsDriverJobAssigned ?? true,
      smsDriverJobCompleted: settings.smsDriverJobCompleted ?? true,
      smsDriverPayrollPaid: settings.smsDriverPayrollPaid ?? true,
      smsClientJobCreated: settings.smsClientJobCreated ?? true,
      smsClientFraudUnderReview: settings.smsClientFraudUnderReview ?? true,
      smsClientDriverAssigned: settings.smsClientDriverAssigned ?? true,
      smsClientStatusOnRoute: settings.smsClientStatusOnRoute ?? true,
      smsClientStatusOnScene: settings.smsClientStatusOnScene ?? true,
      smsClientStatusLoaded: settings.smsClientStatusLoaded ?? true,
      smsClientPaymentLinkCreated: settings.smsClientPaymentLinkCreated ?? true,
      smsClientPaymentSucceeded: settings.smsClientPaymentSucceeded ?? true,
      smsClientPaymentFailed: settings.smsClientPaymentFailed ?? true,
      smsClientJobCancelled: settings.smsClientJobCancelled ?? true,
      smsClientJobCompleted: settings.smsClientJobCompleted ?? true
    });
  }

  private patchEmailFormFromSettings(settings: SystemSettings): void {
    this.emailForm.patchValue({
      emailEnabled: settings.emailEnabled ?? true,
      postmarkServerToken: '',
      postmarkDefaultFromEmail: settings.postmarkDefaultFromEmail || '',
      postmarkMessageStream: settings.postmarkMessageStream || '',
      emailDriverJobAssigned: settings.emailDriverJobAssigned ?? true,
      emailDriverJobCompleted: settings.emailDriverJobCompleted ?? true,
      emailDriverPayrollPaid: settings.emailDriverPayrollPaid ?? true,
      emailClientJobCreated: settings.emailClientJobCreated ?? true,
      emailClientFraudUnderReview: settings.emailClientFraudUnderReview ?? true,
      emailClientDriverAssigned: settings.emailClientDriverAssigned ?? true,
      emailClientStatusOnRoute: settings.emailClientStatusOnRoute ?? true,
      emailClientStatusOnScene: settings.emailClientStatusOnScene ?? true,
      emailClientStatusLoaded: settings.emailClientStatusLoaded ?? true,
      emailClientPaymentLinkCreated: settings.emailClientPaymentLinkCreated ?? true,
      emailClientPaymentSucceeded: settings.emailClientPaymentSucceeded ?? true,
      emailClientPaymentFailed: settings.emailClientPaymentFailed ?? true,
      emailClientJobCancelled: settings.emailClientJobCancelled ?? true,
      emailClientJobCompleted: settings.emailClientJobCompleted ?? true
    });
  }

  openSmsTestModal(): void {
    this.showSmsTestModal = true;
    this.smsTestError = null;
    this.smsTestResult = null;
    this.smsTestForm.patchValue({ toPhone: '', message: '' });
  }

  closeSmsTestModal(): void {
    this.showSmsTestModal = false;
    this.smsTestLoading = false;
    this.smsTestError = null;
    this.smsTestResult = null;
  }

  sendTestSms(): void {
    if (!this.isSuperAdmin()) {
      return;
    }
    if (this.smsTestForm.invalid) {
      this.smsTestForm.markAllAsTouched();
      return;
    }

    const v = this.smsTestForm.getRawValue();
    const message = typeof v.message === 'string' && v.message.trim().length > 0 ? v.message.trim() : undefined;

    this.smsTestLoading = true;
    this.smsTestError = null;
    this.smsTestResult = null;

    this.settingsService
      .testSms({
        toPhone: String(v.toPhone).trim(),
        message
      })
      .pipe(
        finalize(() => {
          this.smsTestLoading = false;
        })
      )
      .subscribe({
        next: (res) => {
          this.smsTestResult = res;
          if (!res.success) {
            this.smsTestError = res.errorMessage || 'SMS could not be sent.';
          }
        },
        error: (err) => {
          const body = err.error;
          this.smsTestError =
            body?.errorMessage ||
            body?.message ||
            body?.error ||
            'Failed to send test SMS.';
        }
      });
  }

  openEmailTestModal(): void {
    this.showEmailTestModal = true;
    this.emailTestError = null;
    this.emailTestResult = null;
    this.emailTestForm.patchValue({ toEmail: '', subject: '', htmlBody: '' });
  }

  closeEmailTestModal(): void {
    this.showEmailTestModal = false;
    this.emailTestLoading = false;
    this.emailTestError = null;
    this.emailTestResult = null;
  }

  sendTestEmail(): void {
    if (!this.isSuperAdmin()) {
      return;
    }
    if (this.emailTestForm.invalid) {
      this.emailTestForm.markAllAsTouched();
      return;
    }

    const v = this.emailTestForm.getRawValue();
    const subject = typeof v.subject === 'string' && v.subject.trim().length > 0 ? v.subject.trim() : undefined;
    const htmlBody = typeof v.htmlBody === 'string' && v.htmlBody.trim().length > 0 ? v.htmlBody.trim() : undefined;

    this.emailTestLoading = true;
    this.emailTestError = null;
    this.emailTestResult = null;

    this.settingsService
      .testEmail({
        toEmail: String(v.toEmail).trim(),
        subject,
        htmlBody
      })
      .pipe(
        finalize(() => {
          this.emailTestLoading = false;
        })
      )
      .subscribe({
        next: (res) => {
          this.emailTestResult = res;
          if (!res.success) {
            this.emailTestError = res.errorMessage || 'Email could not be sent.';
          }
        },
        error: (err) => {
          const body = err.error;
          this.emailTestError =
            body?.errorMessage ||
            body?.message ||
            body?.error ||
            'Failed to send test email.';
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
