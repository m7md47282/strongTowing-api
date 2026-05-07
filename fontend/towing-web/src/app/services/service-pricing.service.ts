import { Injectable } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiService } from './api.service';
import {
  CreateServicePricingProfilePayload,
  ServicePricingProfile,
  UpdateServicePricingProfilePayload
} from '../models/service-pricing.model';

function numOrNull(raw: unknown): number | null {
  if (raw === null || raw === undefined || raw === '') {
    return null;
  }
  const n = Number(raw);
  return Number.isFinite(n) ? n : null;
}

function mapProfileDto(raw: Record<string, unknown>): ServicePricingProfile {
  return {
    id: Number(raw['id'] ?? raw['Id']),
    name: String(raw['name'] ?? raw['Name'] ?? ''),
    basePrice: Number(raw['basePrice'] ?? raw['BasePrice'] ?? 0),
    pricePerMile: Number(raw['pricePerMile'] ?? raw['PricePerMile'] ?? 0),
    enroutePricePerMile: numOrNull(raw['enroutePricePerMile'] ?? raw['EnroutePricePerMile']),
    loadedPricePerMile: numOrNull(raw['loadedPricePerMile'] ?? raw['LoadedPricePerMile']),
    deadheadPricePerMile: numOrNull(raw['deadheadPricePerMile'] ?? raw['DeadheadPricePerMile']),
    isAvailable: Boolean(raw['isAvailable'] ?? raw['IsAvailable']),
    createdAt: String(raw['createdAt'] ?? raw['CreatedAt'] ?? ''),
    updatedAt: String(raw['updatedAt'] ?? raw['UpdatedAt'] ?? '')
  };
}

@Injectable({
  providedIn: 'root'
})
export class ServicePricingService {
  constructor(private api: ApiService) {}

  getAll(includeUnavailable = false): Observable<ServicePricingProfile[]> {
    let params = new HttpParams();
    if (includeUnavailable) {
      params = params.set('includeUnavailable', 'true');
    }

    return this.api
      .get<Record<string, unknown>[]>('servicepricing', params)
      .pipe(map((rows) => rows.map((row) => mapProfileDto(row))));
  }

  getByName(name: string): Observable<ServicePricingProfile> {
    return this.api
      .get<Record<string, unknown>>(`servicepricing/by-name/${encodeURIComponent(name)}`)
      .pipe(map((row) => mapProfileDto(row)));
  }

  setAvailability(id: number, isAvailable: boolean): Observable<ServicePricingProfile> {
    return this.api
      .put<Record<string, unknown>>(`servicepricing/${id}/availability`, isAvailable)
      .pipe(map((row) => mapProfileDto(row)));
  }

  create(payload: CreateServicePricingProfilePayload): Observable<ServicePricingProfile> {
    return this.api
      .post<Record<string, unknown>>('servicepricing', payload)
      .pipe(map((row) => mapProfileDto(row)));
  }

  update(id: number, payload: UpdateServicePricingProfilePayload): Observable<ServicePricingProfile> {
    return this.api
      .put<Record<string, unknown>>(`servicepricing/${id}`, payload)
      .pipe(map((row) => mapProfileDto(row)));
  }

  delete(id: number): Observable<void> {
    return this.api.delete<void>(`servicepricing/${id}`);
  }
}
