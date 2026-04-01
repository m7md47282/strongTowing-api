import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ApiService } from './api.service';

export interface DriverPayrollListItem {
  id: number;
  payPeriodStart: string;
  payPeriodEnd: string;
  totalJobs: number;
  totalJobRevenue: number;
  commissionPercentage: number;
  grossEarnings: number;
  netPay: number;
  status: string;
  paidAt: string | null;
}

export interface DriverEarningsSummary {
  completedJobsCount: number;
  completedJobsTotalRevenue: number;
  payrolls: DriverPayrollListItem[];
}

@Injectable({
  providedIn: 'root'
})
export class DriversService {
  constructor(private apiService: ApiService) {}

  getMyEarnings(): Observable<DriverEarningsSummary> {
    return this.apiService.get<DriverEarningsSummary>('drivers/me/earnings').pipe(
      catchError((error) => {
        console.error('Driver earnings error:', error);
        return throwError(() => error);
      })
    );
  }
}
