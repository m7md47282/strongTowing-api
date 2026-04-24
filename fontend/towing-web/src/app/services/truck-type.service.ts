import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ApiService } from './api.service';

export interface TruckType {
  id: number;
  name: string;
  description?: string | null;
  createdAt: string;
}

export interface CreateTruckTypeRequest {
  name: string;
  description?: string;
}

export interface UpdateTruckTypeRequest {
  name: string;
  description?: string;
}

@Injectable({
  providedIn: 'root'
})
export class TruckTypeService {
  constructor(private apiService: ApiService) {}

  getAll(): Observable<TruckType[]> {
    return this.apiService.get<TruckType[]>('truck-types').pipe(
      catchError((error) => {
        console.error('Get truck types error:', error);
        return throwError(() => error);
      })
    );
  }

  getById(id: number): Observable<TruckType> {
    return this.apiService.get<TruckType>(`truck-types/${id}`).pipe(
      catchError((error) => {
        console.error('Get truck type error:', error);
        return throwError(() => error);
      })
    );
  }

  create(body: CreateTruckTypeRequest): Observable<TruckType> {
    return this.apiService.post<TruckType>('truck-types', body).pipe(
      catchError((error) => {
        console.error('Create truck type error:', error);
        return throwError(() => error);
      })
    );
  }

  update(id: number, body: UpdateTruckTypeRequest): Observable<TruckType> {
    return this.apiService.put<TruckType>(`truck-types/${id}`, body).pipe(
      catchError((error) => {
        console.error('Update truck type error:', error);
        return throwError(() => error);
      })
    );
  }

  delete(id: number): Observable<void> {
    return this.apiService.delete<void>(`truck-types/${id}`).pipe(
      catchError((error) => {
        console.error('Delete truck type error:', error);
        return throwError(() => error);
      })
    );
  }
}
