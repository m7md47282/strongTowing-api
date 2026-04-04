import { Injectable } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiService } from './api.service';

/** Paginated models response (API camelCase JSON). */
export interface VehicleModelsPage {
  models: string[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

/** US-market makes/models via backend proxy to NHTSA vPIC (server caches NHTSA responses). */
@Injectable({
  providedIn: 'root'
})
export class VehicleCatalogService {
  constructor(private api: ApiService) {}

  getMakes(): Observable<string[]> {
    return this.api
      .get<{ makes: string[] }>('vehicle-catalog/makes')
      .pipe(map((r) => r.makes ?? []));
  }

  /** One page of models; backend caches full list per make. */
  getModelsPage(makeName: string, page = 1, pageSize = 100): Observable<VehicleModelsPage> {
    const params = new HttpParams()
      .set('makeName', makeName)
      .set('page', String(page))
      .set('pageSize', String(pageSize));
    return this.api.get<VehicleModelsPage>('vehicle-catalog/models', params);
  }
}
