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
  pricingFreeMiles: number;
  maxDiscountPercent: number;
  allowManualTotalOverride: boolean;
  manualOverrideRequiresReason: boolean;
  pricingMismatchTolerance: number;
  pricingRoundingMode: 'AwayFromZero' | 'ToEven';
  officeLatitude: number | null;
  officeLongitude: number | null;

  smsEnabled: boolean;
  smsTwilioAccountSid?: string | null;
  smsTwilioAuthTokenConfigured: boolean;
  smsTwilioFromNumber?: string | null;
  smsTwilioMessagingServiceSid?: string | null;

  smsDriverJobAssigned: boolean;
  smsDriverJobCompleted: boolean;
  smsDriverPayrollPaid: boolean;

  smsClientJobCreated: boolean;
  smsClientFraudUnderReview: boolean;
  smsClientDriverAssigned: boolean;
  smsClientStatusOnRoute: boolean;
  smsClientStatusOnScene: boolean;
  smsClientStatusLoaded: boolean;
  smsClientPaymentLinkCreated: boolean;
  smsClientPaymentSucceeded: boolean;
  smsClientPaymentFailed: boolean;
  smsClientJobCancelled: boolean;
  smsClientJobCompleted: boolean;

  emailEnabled: boolean;
  postmarkServerTokenConfigured: boolean;
  postmarkDefaultFromEmail?: string | null;
  postmarkMessageStream?: string | null;

  emailDriverJobAssigned: boolean;
  emailDriverJobCompleted: boolean;
  emailDriverPayrollPaid: boolean;

  emailClientJobCreated: boolean;
  emailClientFraudUnderReview: boolean;
  emailClientDriverAssigned: boolean;
  emailClientStatusOnRoute: boolean;
  emailClientStatusOnScene: boolean;
  emailClientStatusLoaded: boolean;
  emailClientPaymentLinkCreated: boolean;
  emailClientPaymentSucceeded: boolean;
  emailClientPaymentFailed: boolean;
  emailClientJobCancelled: boolean;
  emailClientJobCompleted: boolean;

  updatedAt: string;
  updatedBy: string;
}

export interface DispatchContact {
  displayName: string;
  phone: string | null;
}

export interface TestSmsResponse {
  success: boolean;
  toE164?: string | null;
  twilioMessageSid?: string | null;
  errorMessage?: string | null;
}

export interface TestSmsRequest {
  toPhone: string;
  message?: string | null;
}

export interface TestEmailResponse {
  success: boolean;
  toEmail?: string | null;
  postmarkMessageId?: string | null;
  errorMessage?: string | null;
}

export interface TestEmailRequest {
  toEmail: string;
  subject?: string | null;
  htmlBody?: string | null;
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
  pricingFreeMiles: number;
  maxDiscountPercent: number;
  allowManualTotalOverride: boolean;
  manualOverrideRequiresReason: boolean;
  pricingMismatchTolerance: number;
  pricingRoundingMode: 'AwayFromZero' | 'ToEven';
  officeLatitude: number | null;
  officeLongitude: number | null;

  smsEnabled: boolean;
  smsTwilioAccountSid?: string | null;
  smsTwilioAuthToken?: string | null;
  smsTwilioFromNumber?: string | null;
  smsTwilioMessagingServiceSid?: string | null;

  smsDriverJobAssigned: boolean;
  smsDriverJobCompleted: boolean;
  smsDriverPayrollPaid: boolean;

  smsClientJobCreated: boolean;
  smsClientFraudUnderReview: boolean;
  smsClientDriverAssigned: boolean;
  smsClientStatusOnRoute: boolean;
  smsClientStatusOnScene: boolean;
  smsClientStatusLoaded: boolean;
  smsClientPaymentLinkCreated: boolean;
  smsClientPaymentSucceeded: boolean;
  smsClientPaymentFailed: boolean;
  smsClientJobCancelled: boolean;
  smsClientJobCompleted: boolean;

  emailEnabled: boolean;
  postmarkServerToken?: string | null;
  postmarkDefaultFromEmail?: string | null;
  postmarkMessageStream?: string | null;

  emailDriverJobAssigned: boolean;
  emailDriverJobCompleted: boolean;
  emailDriverPayrollPaid: boolean;

  emailClientJobCreated: boolean;
  emailClientFraudUnderReview: boolean;
  emailClientDriverAssigned: boolean;
  emailClientStatusOnRoute: boolean;
  emailClientStatusOnScene: boolean;
  emailClientStatusLoaded: boolean;
  emailClientPaymentLinkCreated: boolean;
  emailClientPaymentSucceeded: boolean;
  emailClientPaymentFailed: boolean;
  emailClientJobCancelled: boolean;
  emailClientJobCompleted: boolean;
}

export interface EmailTemplateItem {
  eventKey: string;
  displayName: string;
  description: string;
  placeholders: string[];
  subject: string;
  htmlBody: string;
  textBody: string | null;
  isCustom: boolean;
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

  /** SuperAdmin: send a test SMS using saved Twilio credentials. */
  testSms(payload: TestSmsRequest): Observable<TestSmsResponse> {
    return this.apiService.post<TestSmsResponse>('settings/test-sms', payload);
  }

  /** SuperAdmin: send a test email using saved Postmark credentials. */
  testEmail(payload: TestEmailRequest): Observable<TestEmailResponse> {
    return this.apiService.post<TestEmailResponse>('settings/test-email', payload);
  }

  /** Public: dispatch phone from appsettings (for driver app). */
  getDispatchContact(): Observable<DispatchContact> {
    return this.apiService.getPublic<DispatchContact>('settings/dispatch-contact');
  }

  /** Admin/SuperAdmin: list transactional email templates. */
  getEmailTemplates(): Observable<EmailTemplateItem[]> {
    return this.apiService.get<EmailTemplateItem[]>('settings/email-templates');
  }

  /** SuperAdmin: upsert one or more custom templates. */
  updateEmailTemplates(
    items: { eventKey: string; subject: string; htmlBody: string; textBody?: string | null }[]
  ): Observable<void> {
    return this.apiService.put<void>('settings/email-templates', { items });
  }

  /** SuperAdmin: remove custom row for an event (revert to code defaults). */
  resetEmailTemplate(eventKey: string): Observable<void> {
    return this.apiService.post<void>('settings/email-templates/reset', { eventKey });
  }
}
