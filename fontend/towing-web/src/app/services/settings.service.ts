import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

export interface SystemSettings {
  id: number;
  driverCommissionPercentage: number;
  payPeriodType: string;
  stripePublicKey?: string | null;
  stripeEnabled: boolean;
  stripeSecretKeyConfigured: boolean;
  stripeWebhookConfigured: boolean;
  stripeTestPublicKey?: string | null;
  stripeTestSecretKeyConfigured: boolean;
  stripeTestWebhookConfigured: boolean;
  stripeLivePublicKey?: string | null;
  stripeLiveSecretKeyConfigured: boolean;
  stripeLiveWebhookConfigured: boolean;
  stripeMode: 'test' | 'live';
  preAuthorizationEnabled: boolean;
  preAuthorizationMinAmount: number;
  preAuthorizationMaxAmount: number;
  fraudReviewScoreThreshold: number;
  duplicateRequestWindowMinutes: number;
  cancelFeeBeforeDispatchPercent: number;
  cancelFeeAfterDispatchPercent: number;
  cancelFeeAfterArrivalPercent: number;
  defaultPricingTaxPercent: number;
  defaultPricingServiceChargePercent: number;
  defaultPricingHookupFee: number;
  maxDiscountPercent: number;
  allowManualTotalOverride: boolean;
  manualOverrideRequiresReason: boolean;
  pricingMismatchTolerance: number;
  pricingRoundingMode: 'AwayFromZero' | 'ToEven';
  officeLatitude: number | null;
  officeLongitude: number | null;
  updatedAt: string;
  updatedBy: string;
}

export interface DispatchContact {
  displayName: string;
  phone: string | null;
}

export interface UpdateSystemSettingsRequest {
  driverCommissionPercentage: number;
  stripePublicKey?: string | null;
  stripeSecretKey?: string | null;
  stripeWebhookSecret?: string | null;
  stripeEnabled: boolean;
  stripeTestPublicKey?: string | null;
  stripeTestSecretKey?: string | null;
  stripeTestWebhookSecret?: string | null;
  stripeLivePublicKey?: string | null;
  stripeLiveSecretKey?: string | null;
  stripeLiveWebhookSecret?: string | null;
  stripeMode: 'test' | 'live';
  preAuthorizationEnabled: boolean;
  preAuthorizationMinAmount: number;
  preAuthorizationMaxAmount: number;
  fraudReviewScoreThreshold: number;
  duplicateRequestWindowMinutes: number;
  cancelFeeBeforeDispatchPercent: number;
  cancelFeeAfterDispatchPercent: number;
  cancelFeeAfterArrivalPercent: number;
  defaultPricingTaxPercent: number;
  defaultPricingServiceChargePercent: number;
  defaultPricingHookupFee: number;
  maxDiscountPercent: number;
  allowManualTotalOverride: boolean;
  manualOverrideRequiresReason: boolean;
  pricingMismatchTolerance: number;
  pricingRoundingMode: 'AwayFromZero' | 'ToEven';
  officeLatitude: number | null;
  officeLongitude: number | null;
}

@Injectable({
  providedIn: 'root'
})
export class SettingsService {
  constructor(private apiService: ApiService) {}

  getSettings(): Observable<SystemSettings> {
    return this.apiService.get<SystemSettings>('settings');
  }

  updateSettings(payload: UpdateSystemSettingsRequest): Observable<SystemSettings> {
    return this.apiService.put<SystemSettings>('settings', payload);
  }

  /** Public: dispatch phone from appsettings (for driver app). */
  getDispatchContact(): Observable<DispatchContact> {
    return this.apiService.getPublic<DispatchContact>('settings/dispatch-contact');
  }
}
