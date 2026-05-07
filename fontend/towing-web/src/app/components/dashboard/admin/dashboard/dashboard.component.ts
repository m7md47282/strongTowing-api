import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../../services/auth.service';
import { AdminDashboardSummary, JobService, Job, JOB_STATUS } from '../../../../services/job.service';
import { PaymentService } from '../../../../services/payment.service';
import { ApiService } from '../../../../services/api.service';
import { User } from '../../../../models/user.model';
import { HttpParams } from '@angular/common/http';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ChartConfiguration, ChartOptions, Chart, registerables } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';

// Register Chart.js components
if (typeof Chart !== 'undefined') {
  Chart.register(...registerables);
}

interface PagedResponse<T> {
  data: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

interface DashboardStats {
  activeRequests: number;
  completedToday: number;
  monthlyRevenue: number;
  activeDrivers: number;
  activeRequestsChange: number;
  completedChange: number;
  revenueChange: number;
  driversChange: number;
}

interface ServiceTypeBreakdown {
  towing: number;
  roadside: number;
  jumpStart: number;
  tireChange: number;
  other: number;
  total: number;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, BaseChartDirective],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  user: User | null = null;
  loading = true;
  error: string | null = null;

  // Statistics
  stats: DashboardStats = {
    activeRequests: 0,
    completedToday: 0,
    monthlyRevenue: 0,
    activeDrivers: 0,
    activeRequestsChange: 0,
    completedChange: 0,
    revenueChange: 0,
    driversChange: 0
  };

  recentActiveJobs: Job[] = [];

  // Service type breakdown
  serviceBreakdown: ServiceTypeBreakdown = {
    towing: 0,
    roadside: 0,
    jumpStart: 0,
    tireChange: 0,
    other: 0,
    total: 0
  };

  // Chart data for service requests overview
  public lineChartData: ChartConfiguration<'line'>['data'] = {
    labels: [],
    datasets: [
      {
        data: [],
        label: 'Service Requests',
        borderColor: 'rgb(59, 130, 246)',
        backgroundColor: 'rgba(59, 130, 246, 0.1)',
        fill: true,
        tension: 0.4
      }
    ]
  };

  public lineChartOptions: ChartOptions<'line'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: false
      },
      tooltip: {
        backgroundColor: 'rgba(0, 0, 0, 0.8)',
        padding: 12,
        titleFont: {
          size: 14
        },
        bodyFont: {
          size: 12
        }
      }
    },
    scales: {
      y: {
        beginAtZero: true,
        grid: {
          color: 'rgba(0, 0, 0, 0.05)'
        },
        ticks: {
          stepSize: 1
        }
      },
      x: {
        grid: {
          display: false
        }
      }
    }
  };

  public lineChartType: 'line' = 'line';

  // Pie chart for service type breakdown
  public pieChartData: ChartConfiguration<'pie'>['data'] = {
    labels: ['Towing', 'Roadside Assistance', 'Jump Start', 'Tire Change', 'Other'],
    datasets: [{
      data: [0, 0, 0, 0, 0],
      backgroundColor: [
        'rgb(59, 130, 246)',  // Primary blue
        'rgb(249, 115, 22)',  // Orange
        'rgb(59, 130, 246)',  // Blue
        'rgb(34, 197, 94)',   // Green
        'rgb(107, 114, 128)'  // Gray
      ],
      borderWidth: 0
    }]
  };

  public pieChartOptions: ChartConfiguration<'pie'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'bottom',
        labels: {
          padding: 15,
          font: {
            size: 12
          }
        }
      },
      tooltip: {
        backgroundColor: 'rgba(0, 0, 0, 0.8)',
        padding: 12,
        callbacks: {
          label: (context: any) => {
            const label = context.label || '';
            const value = context.parsed || 0;
            const total = context.dataset.data.reduce((a: number, b: number) => a + b, 0);
            const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : '0';
            return `${label}: ${value} (${percentage}%)`;
          }
        }
      }
    }
  };

  public pieChartType: 'pie' = 'pie';

  constructor(
    private authService: AuthService,
    private jobService: JobService,
    private paymentService: PaymentService,
    private apiService: ApiService
  ) {}

  ngOnInit(): void {
    this.user = this.authService.getCurrentUser();
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.loading = true;
    this.error = null;

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const startOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);
    const endOfToday = new Date(today);
    endOfToday.setHours(23, 59, 59, 999);

    const chartPeriodStart = new Date(today);
    chartPeriodStart.setDate(chartPeriodStart.getDate() - 6);
    chartPeriodStart.setHours(0, 0, 0, 0);

    const startOfMonthStr = startOfMonth.toISOString().split('T')[0];
    const endOfMonthStr = today.toISOString().split('T')[0];

    const summaryParams: Record<string, string> = {
      monthStart: startOfMonth.toISOString(),
      monthEnd: endOfToday.toISOString(),
      dayStart: today.toISOString(),
      dayEnd: endOfToday.toISOString(),
      chartPeriodStart: chartPeriodStart.toISOString(),
      chartPeriodEnd: endOfToday.toISOString()
    };

    forkJoin({
      summary: this.jobService.getAdminDashboardSummary(summaryParams).pipe(
        catchError(error => {
          console.error('Error loading dashboard summary:', error);
          return of(null as AdminDashboardSummary | null);
        })
      ),
      paymentStats: this.paymentService.getPaymentStatistics({
        startDate: startOfMonthStr,
        endDate: endOfMonthStr
      }).pipe(
        catchError(error => {
          console.error('Error loading payment stats:', error);
          return of({
            totalPayments: 0,
            totalRevenue: 0,
            revenueByMethod: { card: 0, paymentLink: 0, cash: 0 },
            pendingPayments: 0,
            totalCashCollected: 0,
            totalDriverCommissions: 0,
            driverCommissionRatePercent: 0
          });
        })
      ),
      drivers: this.apiService.get<PagedResponse<User>>('users/drivers', new HttpParams()
        .set('pageNumber', '1')
        .set('pageSize', '100')
        .set('isActive', 'true')
        .set('availableForDispatchOnly', 'true')
      ).pipe(
        catchError(error => {
          console.error('Error loading drivers:', error);
          return of({ data: [], pageNumber: 1, pageSize: 100, totalCount: 0, totalPages: 0, hasPreviousPage: false, hasNextPage: false });
        })
      )
    }).subscribe({
      next: (data) => {
        if (!data.summary) {
          this.error = 'Failed to load dashboard metrics.';
          this.loading = false;
          return;
        }
        this.applyDashboardSummary(data.summary, data.paymentStats, data.drivers.data);
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading dashboard data:', error);
        this.error = 'Failed to load dashboard data. Please try again.';
        this.loading = false;
      }
    });
  }

  private applyDashboardSummary(summary: AdminDashboardSummary, paymentStats: any, drivers: User[]): void {
    this.stats.activeRequests = summary.activeRequests;
    this.stats.completedToday = summary.completedToday;
    this.stats.monthlyRevenue = paymentStats.totalRevenue || 0;
    this.stats.activeDrivers = drivers.filter((d) => d.isActive).length;
    this.stats.activeRequestsChange = 0;
    this.stats.completedChange = 0;
    this.stats.revenueChange = 0;
    this.stats.driversChange = 0;

    this.recentActiveJobs = summary.recentActiveJobs || [];

    const sb = summary.serviceBreakdown;
    this.serviceBreakdown = {
      towing: sb?.towing ?? 0,
      roadside: sb?.roadside ?? 0,
      jumpStart: sb?.jumpStart ?? 0,
      tireChange: sb?.tireChange ?? 0,
      other: sb?.other ?? 0,
      total: sb?.total ?? 0
    };

    const daily = summary.jobsCreatedLast7Days || [];
    const lineLabels = daily.map((d) => d.dateLabel);
    const lineCounts = daily.map((d) => d.count);

    this.lineChartData = {
      labels: lineLabels,
      datasets: [
        {
          data: lineCounts,
          label: 'Service Requests',
          borderColor: 'rgb(59, 130, 246)',
          backgroundColor: 'rgba(59, 130, 246, 0.1)',
          fill: true,
          tension: 0.4
        }
      ]
    };

    this.pieChartData = {
      labels: ['Towing', 'Roadside Assistance', 'Jump Start', 'Tire Change', 'Other'],
      datasets: [
        {
          data: [
            this.serviceBreakdown.towing,
            this.serviceBreakdown.roadside,
            this.serviceBreakdown.jumpStart,
            this.serviceBreakdown.tireChange,
            this.serviceBreakdown.other
          ],
          backgroundColor: [
            'rgb(59, 130, 246)',
            'rgb(249, 115, 22)',
            'rgb(59, 130, 246)',
            'rgb(34, 197, 94)',
            'rgb(107, 114, 128)'
          ],
          borderWidth: 0
        }
      ]
    };
  }

  getServiceTypePercentage(type: keyof Omit<ServiceTypeBreakdown, 'total'>): number {
    if (this.serviceBreakdown.total === 0) return 0;
    return Math.round((this.serviceBreakdown[type] / this.serviceBreakdown.total) * 100);
  }

  formatCurrency(amount: number): string {
    if (amount >= 1000) {
      return `$${(amount / 1000).toFixed(1)}K`;
    }
    return `$${amount.toFixed(0)}`;
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true });
  }

  getStatusColor(status: string): string {
    switch (status) {
      case JOB_STATUS.Completed:
        return 'text-green-600';
      case JOB_STATUS.OnScene:
      case JOB_STATUS.OnRoute:
        return 'text-primary';
      case JOB_STATUS.Waiting:
        return 'text-orange-600';
      case JOB_STATUS.Dispatch:
        return 'text-blue-600';
      case JOB_STATUS.Loaded:
        return 'text-teal-600';
      case JOB_STATUS.Cancelled:
        return 'text-red-600';
      default:
        return 'text-gray-600';
    }
  }

  refreshData(): void {
    this.loadDashboardData();
  }
}


