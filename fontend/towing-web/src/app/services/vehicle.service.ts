import { Injectable } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ApiService } from './api.service';

export interface Vehicle {
  id: number;
  ownerId?: string;
  vin: string;
  make: string;
  model: string;
  year: number;
  color: string;
}

export interface CreateVehicleRequest {
  vin: string;
  make: string;
  model: string;
  year: number;
  color?: string;
}

@Injectable({
  providedIn: 'root'
})
export class VehicleService {
  constructor(private apiService: ApiService) {}

  getAllVehicles(ownerId?: string): Observable<Vehicle[]> {
    const params = ownerId ? new HttpParams().set('ownerId', ownerId) : undefined;
    return this.apiService.get<Vehicle[]>('vehicles', params).pipe(
      catchError(error => {
        console.error('Get vehicles error:', error);
        return throwError(() => error);
      })
    );
  }

  getVehicleById(id: number): Observable<Vehicle> {
    return this.apiService.get<Vehicle>(`vehicles/${id}`).pipe(
      catchError(error => {
        console.error('Get vehicle error:', error);
        return throwError(() => error);
      })
    );
  }

  getVehicleByVIN(vin: string): Observable<Vehicle> {
    return this.apiService.get<Vehicle>(`vehicles/vin/${vin}`).pipe(
      catchError(error => {
        console.error('Get vehicle by VIN error:', error);
        return throwError(() => error);
      })
    );
  }

  createVehicle(vehicle: CreateVehicleRequest): Observable<Vehicle> {
    return this.apiService.post<Vehicle>('vehicles', vehicle).pipe(
      catchError(error => {
        console.error('Create vehicle error:', error);
        return throwError(() => error);
      })
    );
  }
}


