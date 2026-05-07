import { Injectable } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiService } from './api.service';
import {
  CreateInsuranceAccountPayload,
  InsuranceAccount,
  UpdateInsuranceAccountPayload
} from '../models/insurance-account.model';
import {
  InsuranceAccountServiceRate,
  UpsertInsuranceAccountServiceRatePayload
} from '../models/insurance-account-service-rate.model';

function s(raw: Record<string, unknown>, camel: string, pascal: string): string | null {
  const v = (raw[camel] ?? raw[pascal]) as string | null | undefined;
  return v == null || v === '' ? null : v;
}

function mapAccountDto(raw: Record<string, unknown>): InsuranceAccount {
  const id = (raw['id'] ?? raw['Id']) as number;
  const name = (raw['name'] ?? raw['Name']) as string;
  return {
    id,
    name,
    accountNumber: s(raw, 'accountNumber', 'AccountNumber'),
    contactName: s(raw, 'contactName', 'ContactName'),
    billingEmail: s(raw, 'billingEmail', 'BillingEmail'),
    billingPhone: s(raw, 'billingPhone', 'BillingPhone'),
    addressLine1: s(raw, 'addressLine1', 'AddressLine1'),
    addressLine2: s(raw, 'addressLine2', 'AddressLine2'),
    city: s(raw, 'city', 'City'),
    state: s(raw, 'state', 'State'),
    postalCode: s(raw, 'postalCode', 'PostalCode'),
    notes: s(raw, 'notes', 'Notes'),
    isActive: Boolean(raw['isActive'] ?? raw['IsActive']),
    rateAB: Number(raw['rateAB'] ?? raw['RateAB'] ?? 0),
    rateBC: Number(raw['rateBC'] ?? raw['RateBC'] ?? 0),
    rateCA: Number(raw['rateCA'] ?? raw['RateCA'] ?? 0),
    createdAt: (raw['createdAt'] ?? raw['CreatedAt']) as string,
    updatedAt: (raw['updatedAt'] ?? raw['UpdatedAt']) as string
  };
}

@Injectable({
  providedIn: 'root'
})
export class AccountsService {
  constructor(private api: ApiService) {}

  getAll(includeInactive = false): Observable<InsuranceAccount[]> {
    let params = new HttpParams();
    if (includeInactive) {
      params = params.set('includeInactive', 'true');
    }
    return this.api
      .get<Record<string, unknown>[]>('accounts', params)
      .pipe(map((rows) => rows.map((r) => mapAccountDto(r))));
  }

  getById(id: number): Observable<InsuranceAccount> {
    return this.api
      .get<Record<string, unknown>>(`accounts/${id}`)
      .pipe(map((r) => mapAccountDto(r)));
  }

  create(body: CreateInsuranceAccountPayload): Observable<InsuranceAccount> {
    return this.api
      .post<Record<string, unknown>>('accounts', body)
      .pipe(map((r) => mapAccountDto(r)));
  }

  update(id: number, body: UpdateInsuranceAccountPayload): Observable<InsuranceAccount> {
    return this.api
      .put<Record<string, unknown>>(`accounts/${id}`, body)
      .pipe(map((r) => mapAccountDto(r)));
  }

  delete(id: number): Observable<void> {
    return this.api.delete<void>(`accounts/${id}`);
  }

  listServiceRates(accountId: number): Observable<InsuranceAccountServiceRate[]> {
    return this.api
      .get<Record<string, unknown>[]>(`accounts/${accountId}/service-rates`)
      .pipe(map((rows) => rows.map((r) => mapServiceRateDto(r))));
  }

  createServiceRate(
    accountId: number,
    body: UpsertInsuranceAccountServiceRatePayload
  ): Observable<InsuranceAccountServiceRate> {
    return this.api
      .post<Record<string, unknown>>(`accounts/${accountId}/service-rates`, body)
      .pipe(map((r) => mapServiceRateDto(r)));
  }

  updateServiceRate(
    accountId: number,
    rateId: number,
    body: UpsertInsuranceAccountServiceRatePayload
  ): Observable<InsuranceAccountServiceRate> {
    return this.api
      .put<Record<string, unknown>>(`accounts/${accountId}/service-rates/${rateId}`, body)
      .pipe(map((r) => mapServiceRateDto(r)));
  }

  deleteServiceRate(accountId: number, rateId: number): Observable<void> {
    return this.api.delete<void>(`accounts/${accountId}/service-rates/${rateId}`);
  }
}

function mapServiceRateDto(raw: Record<string, unknown>): InsuranceAccountServiceRate {
  const optNum = (k: string, k2: string): number | null => {
    const v = raw[k] ?? raw[k2];
    if (v === null || v === undefined || v === '') {
      return null;
    }
    const n = Number(v);
    return Number.isFinite(n) ? n : null;
  };
  return {
    id: Number(raw['id'] ?? raw['Id']),
    insuranceAccountId: Number(raw['insuranceAccountId'] ?? raw['InsuranceAccountId']),
    servicePricingProfileId: Number(raw['servicePricingProfileId'] ?? raw['ServicePricingProfileId']),
    serviceName: String(raw['serviceName'] ?? raw['ServiceName'] ?? ''),
    basePrice: Number(raw['basePrice'] ?? raw['BasePrice'] ?? 0),
    pricePerMile: Number(raw['pricePerMile'] ?? raw['PricePerMile'] ?? 0),
    enroutePricePerMile: optNum('enroutePricePerMile', 'EnroutePricePerMile'),
    loadedPricePerMile: optNum('loadedPricePerMile', 'LoadedPricePerMile'),
    deadheadPricePerMile: optNum('deadheadPricePerMile', 'DeadheadPricePerMile'),
    createdAt: String(raw['createdAt'] ?? raw['CreatedAt'] ?? ''),
    updatedAt: String(raw['updatedAt'] ?? raw['UpdatedAt'] ?? '')
  };
}
