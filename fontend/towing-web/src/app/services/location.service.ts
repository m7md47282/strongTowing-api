import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { OfficeLocationDto, RouteResponseDto } from '../models/location.model';

@Injectable({
  providedIn: 'root'
})
export class LocationService {
  private readonly baseUrl = (environment.apiUrl || '').replace(/\/$/, '');

  constructor(private http: HttpClient) {}

  getOfficeLocation(): Observable<OfficeLocationDto> {
    return this.http.get<OfficeLocationDto>(`${this.baseUrl}/location/office`);
  }

  calculateDistance(latitude: number, longitude: number): Observable<RouteResponseDto> {
    return this.http.post<RouteResponseDto>(`${this.baseUrl}/location/calculate-distance`, {
      latitude,
      longitude
    });
  }

  /** Driver: send current GPS to the server (for dispatch visibility). */
  pingDriverLocation(latitude: number, longitude: number): Observable<void> {
    const token = typeof localStorage !== 'undefined' ? localStorage.getItem('stongTowing_token') : null;
    const headers = new HttpHeaders({
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {})
    });
    return this.http.post<void>(
      `${this.baseUrl}/location/driver/ping`,
      { latitude, longitude },
      { headers }
    );
  }
}
