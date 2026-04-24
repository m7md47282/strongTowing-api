import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

export interface PricingQuoteRequest {
  accountId?: number;
  accountName?: string;
  servicePricingProfileId?: number;
  serviceName?: string;
  milesAB: number;
  milesBC: number;
  milesCA: number;
  extraItemsTotal: number;
  discountAmount: number;
  discountPercent?: number;
  taxExempt?: boolean;
  hookupFee?: number;
  rateAB?: number;
  rateBC?: number;
  rateCA?: number;
  serviceChargePercent?: number;
  taxPercent?: number;
  manualTotalOverride?: number;
  manualOverrideReason?: string;
}

export interface PricingQuoteResponse {
  accountId?: number | null;
  accountName?: string | null;
  milesAB: number;
  milesBC: number;
  milesCA: number;
  billableMiles: number;
  freeMilesApplied: number;
  pricingFreeMilesAllowance: number;
  serviceBasePrice: number;
  hookupFee: number;
  rateAB: number;
  rateBC: number;
  rateCA: number;
  chargeAB: number;
  chargeBC: number;
  chargeCA: number;
  extraItemsTotal: number;
  baseSubtotal: number;
  discountAmount: number;
  afterDiscount: number;
  serviceChargePercent: number;
  serviceChargeAmount: number;
  taxExempt: boolean;
  taxPercent: number;
  taxableAmount: number;
  taxAmount: number;
  grandTotal: number;
  manualTotalOverrideApplied: boolean;
  manualTotalOverride?: number | null;
  manualOverrideReason?: string | null;
  maxDiscountPercent: number;
  allowManualTotalOverride: boolean;
  manualOverrideRequiresReason: boolean;
  pricingMismatchTolerance: number;
  pricingRoundingMode: 'AwayFromZero' | 'ToEven' | string;
}

@Injectable({
  providedIn: 'root'
})
export class PricingService {
  constructor(private apiService: ApiService) {}

  quote(payload: PricingQuoteRequest): Observable<PricingQuoteResponse> {
    return this.apiService.post<PricingQuoteResponse>('pricing/quote', payload);
  }
}
