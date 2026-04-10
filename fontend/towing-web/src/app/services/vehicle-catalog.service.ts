import { Injectable } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

export interface VehicleCatalogMakeItem {
  id: number;
  name: string;
}

export interface VehicleCatalogPagedMakes {
  items: VehicleCatalogMakeItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  catalogEmpty: boolean;
}

export interface VehicleCatalogModelItem {
  id: number;
  name: string;
}

export interface VehicleCatalogPagedModels {
  items: VehicleCatalogModelItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  catalogEmpty: boolean;
}

/** US-market makes/models from local DB (sync from NHTSA via Admin settings). */
@Injectable({
  providedIn: 'root'
})
export class VehicleCatalogService {
  constructor(private api: ApiService) {}

  searchMakes(q: string, page = 1, pageSize = 30): Observable<VehicleCatalogPagedMakes> {
    let params = new HttpParams().set('page', String(page)).set('pageSize', String(pageSize));
    const t = (q ?? '').trim();
    if (t) {
      params = params.set('q', t);
    }
    return this.api.get<VehicleCatalogPagedMakes>('vehicle-catalog/makes', params);
  }

  searchModels(makeId: number, q: string, page = 1, pageSize = 30): Observable<VehicleCatalogPagedModels> {
    let params = new HttpParams()
      .set('makeId', String(makeId))
      .set('page', String(page))
      .set('pageSize', String(pageSize));
    const t = (q ?? '').trim();
    if (t) {
      params = params.set('q', t);
    }
    return this.api.get<VehicleCatalogPagedModels>('vehicle-catalog/models', params);
  }
}
