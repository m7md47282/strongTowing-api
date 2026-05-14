import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private baseUrl = environment.apiUrl ;

  constructor(private http: HttpClient) { }

  private getHeaders(includeAuth: boolean = true): HttpHeaders {
    const token = localStorage.getItem('stongTowing_token');
    const headers: { [key: string]: string } = {
      'Content-Type': 'application/json'
    };
    
    if (includeAuth && token) {
      headers['Authorization'] = `Bearer ${token}`;
    }
    
    return new HttpHeaders(headers);
  }

  // Generic HTTP methods
  get<T>(endpoint: string, params?: HttpParams, includeAuth = true): Observable<T> {
    return this.http.get<T>(`${this.baseUrl}/${endpoint}`, {
      headers: this.getHeaders(includeAuth),
      params
    });
  }

  /** GET without Authorization (public endpoints). */
  getPublic<T>(endpoint: string, params?: HttpParams): Observable<T> {
    return this.get<T>(endpoint, params, false);
  }

  post<T>(endpoint: string, data: any, includeAuth: boolean = true): Observable<T> {
    return this.http.post<T>(`${this.baseUrl}/${endpoint}`, data, {
      headers: this.getHeaders(includeAuth)
    });
  }

  /** POST JSON and receive a binary body (e.g. PDF). */
  postBlob(endpoint: string, data: unknown, includeAuth = true): Observable<Blob> {
    return this.http.post(`${this.baseUrl}/${endpoint}`, data, {
      headers: this.getHeaders(includeAuth),
      responseType: 'blob'
    });
  }

  put<T>(endpoint: string, data: any): Observable<T> {
    return this.http.put<T>(`${this.baseUrl}/${endpoint}`, data, {
      headers: this.getHeaders()
    });
  }

  patch<T>(endpoint: string, data: unknown): Observable<T> {
    return this.http.patch<T>(`${this.baseUrl}/${endpoint}`, data, {
      headers: this.getHeaders()
    });
  }

  delete<T>(endpoint: string): Observable<T> {
    return this.http.delete<T>(`${this.baseUrl}/${endpoint}`, {
      headers: this.getHeaders()
    });
  }

  // File upload
  uploadFile(endpoint: string, formData: FormData): Observable<any> {
    const token = localStorage.getItem('stongTowing_token');
    const headers = new HttpHeaders({
      'Authorization': token ? `Bearer ${token}` : ''
    });

    return this.http.post(`${this.baseUrl}/${endpoint}`, formData, { headers });
  }

  // Health check
  healthCheck(): Observable<any> {
    return this.http.get(`${this.baseUrl}/health`);
  }
}