import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
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

export interface DriverCashCollectionItem {
  jobId: number;
  completedAt: string | null;
  cashCollected: number;
  payrollDeductionAmount: number;
  payrollDeductionRecorded: boolean;
}

export interface DriverEarningsSummary {
  completedJobsCount: number;
  completedJobsTotalRevenue: number;
  /** Current system-wide rate from settings (same as payroll generation). */
  currentDriverCommissionPercentage: number;
  payrolls: DriverPayrollListItem[];
  /** Completed jobs paid as cash-to-driver (held for company; withheld from payroll). */
  cashCollectionJobsCount: number;
  totalCashCollected: number;
  totalPayrollDeductionFromCash: number;
  recentCashCollections: DriverCashCollectionItem[];
}

@Injectable({
  providedIn: 'root'
})
export class DriversService {
  constructor(private apiService: ApiService) {}

  getMyEarnings(): Observable<DriverEarningsSummary> {
    return this.apiService.get<DriverEarningsSummary>('drivers/me/earnings').pipe(
      map((d) => ({
        ...d,
        currentDriverCommissionPercentage: d.currentDriverCommissionPercentage ?? 30,
        cashCollectionJobsCount: d.cashCollectionJobsCount ?? 0,
        totalCashCollected: d.totalCashCollected ?? 0,
        totalPayrollDeductionFromCash: d.totalPayrollDeductionFromCash ?? 0,
        recentCashCollections: d.recentCashCollections ?? []
      })),
      catchError((error) => {
        console.error('Driver earnings error:', error);
        return throwError(() => error);
      })
    );
  }
}
