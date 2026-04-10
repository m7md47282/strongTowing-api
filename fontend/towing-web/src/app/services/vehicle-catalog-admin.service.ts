import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

/** Matches API VehicleCatalogSyncStatusDto (camelCase JSON). */
export interface VehicleCatalogSyncStatus {
  status: number;
  lastSyncStartedUtc: string | null;
  lastSyncCompletedUtc: string | null;
  lastSyncError: string | null;
  makesCount: number;
  modelsCount: number;
  /** NHTSA make count for current/last run (progress denominator). */
  totalMakes: number;
  /** Makes finished in the per-make model fetch loop. */
  makesProcessed: number;
  /** Model rows inserted so far this run. */
  modelsAddedSoFar: number;
}

@Injectable({
  providedIn: 'root'
})
export class VehicleCatalogAdminService {
  constructor(private api: ApiService) {}

  getSyncStatus(): Observable<VehicleCatalogSyncStatus> {
    return this.api.get<VehicleCatalogSyncStatus>('vehicle-catalog/sync-status');
  }

  postSync(): Observable<{ message?: string }> {
    return this.api.post<{ message?: string }>('vehicle-catalog/sync', {});
  }
}
