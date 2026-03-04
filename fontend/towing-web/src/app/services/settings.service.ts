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
  updatedAt: string;
  updatedBy: string;
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
}
