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
    hookupFee: Number(raw['hookupFee'] ?? raw['HookupFee'] ?? 0),
    rateAB: Number(raw['rateAB'] ?? raw['RateAB'] ?? 0),
    rateBC: Number(raw['rateBC'] ?? raw['RateBC'] ?? 0),
    rateCA: Number(raw['rateCA'] ?? raw['RateCA'] ?? 0),
    serviceChargePercent: Number(raw['serviceChargePercent'] ?? raw['ServiceChargePercent'] ?? 0),
    taxPercent: Number(raw['taxPercent'] ?? raw['TaxPercent'] ?? 0),
    isTaxExemptByDefault: Boolean(raw['isTaxExemptByDefault'] ?? raw['IsTaxExemptByDefault']),
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
}
