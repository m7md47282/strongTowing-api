import { Injectable } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ApiService } from './api.service';

export interface TruckActiveJob {
  jobId: number;
  status: string;
  driverId?: string | null;
  driverName?: string | null;
}

export interface TruckListItem {
  id: number;
  truckTypeId: number;
  truckTypeName: string;
  unitLabel: string;
  licensePlate?: string | null;
  vin?: string | null;
  make?: string | null;
  model?: string | null;
  year?: number | null;
  notes?: string | null;
  isOutOfService: boolean;
  isActive: boolean;
  availabilityLabel: string;
  activeJobs: TruckActiveJob[];
  createdAt: string;
}

export interface CreateTruckRequest {
  truckTypeId: number;
  unitLabel: string;
  licensePlate?: string;
  vin?: string;
  make?: string;
  model?: string;
  year?: number | null;
  notes?: string;
  isOutOfService: boolean;
}

export interface UpdateTruckRequest {
  truckTypeId: number;
  unitLabel: string;
  licensePlate?: string;
  vin?: string;
  make?: string;
  model?: string;
  year?: number | null;
  notes?: string;
  isOutOfService: boolean;
  isActive: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class TruckService {
  constructor(private apiService: ApiService) {}

  getAll(includeInactive = false): Observable<TruckListItem[]> {
    const params = includeInactive ? new HttpParams().set('includeInactive', 'true') : undefined;
    return this.apiService.get<TruckListItem[]>('trucks', params).pipe(
      catchError((error) => {
        console.error('Get trucks error:', error);
        return throwError(() => error);
      })
    );
  }

  getForJobDropdowns(): Observable<TruckListItem[]> {
    return this.apiService.get<TruckListItem[]>('trucks/for-jobs').pipe(
      catchError((error) => {
        console.error('Get trucks for jobs error:', error);
        return throwError(() => error);
      })
    );
  }

  getById(id: number): Observable<TruckListItem> {
    return this.apiService.get<TruckListItem>(`trucks/${id}`).pipe(
      catchError((error) => {
        console.error('Get truck error:', error);
        return throwError(() => error);
      })
    );
  }

  create(body: CreateTruckRequest): Observable<TruckListItem> {
    return this.apiService.post<TruckListItem>('trucks', body).pipe(
      catchError((error) => {
        console.error('Create truck error:', error);
        return throwError(() => error);
      })
    );
  }

  update(id: number, body: UpdateTruckRequest): Observable<TruckListItem> {
    return this.apiService.put<TruckListItem>(`trucks/${id}`, body).pipe(
      catchError((error) => {
        console.error('Update truck error:', error);
        return throwError(() => error);
      })
    );
  }

  delete(id: number): Observable<void> {
    return this.apiService.delete<void>(`trucks/${id}`).pipe(
      catchError((error) => {
        console.error('Delete truck error:', error);
        return throwError(() => error);
      })
    );
  }
}
