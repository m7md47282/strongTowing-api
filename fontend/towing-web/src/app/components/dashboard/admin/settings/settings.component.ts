import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { RoleId } from '../../../../constants/user-roles.constants';
import { AuthService } from '../../../../services/auth.service';
import { SettingsService, SystemSettings, UpdateSystemSettingsRequest } from '../../../../services/settings.service';
import {
  CreatePaymentIntentResponse,
  CreateStripePaymentLinkResponse,
  PaymentService
} from '../../../../services/payment.service';

type SettingsTab = 'general' | 'payments';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss']
})
export class SettingsComponent implements OnInit {
  activeTab: SettingsTab = 'payments';
  settingsForm: FormGroup;
  paymentTestForm: FormGroup;
  loading = false;
  saving = false;
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
    private paymentService: PaymentService
  ) {
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
      stripeLiveWebhookSecret: ['']
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
  }

  setStripeMode(mode: 'test' | 'live'): void {
    this.settingsForm.patchValue({ stripeMode: mode });
  }

  isStripeMode(mode: 'test' | 'live'): boolean {
    return this.settingsForm.get('stripeMode')?.value === mode;
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
            stripeLivePublicKey: settings.stripeLivePublicKey || ''
          });
        },
        error: (err) => {
          this.error = err.error?.message || err.error?.error || 'Failed to load settings.';
        }
      });
  }

  saveSettings(): void {
    if (!this.isSuperAdmin()) {
      this.error = 'Only Super Admin can update settings.';
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
      stripeLiveWebhookSecret: formValue.stripeLiveWebhookSecret || null
    };

    this.settingsService.updateSettings(payload)
      .pipe(finalize(() => { this.saving = false; }))
      .subscribe({
        next: (settings) => {
          this.currentSettings = settings;
          this.successMessage = 'Settings saved successfully.';

          // Clear secret fields after successful save for better security UX.
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
