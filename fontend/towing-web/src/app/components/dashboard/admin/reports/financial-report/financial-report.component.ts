import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { ReportsService, FinancialReportSummary } from '../../../../../services/reports.service';
import { BaseChartDirective } from 'ng2-charts';
import { Chart, ChartConfiguration, ChartOptions, registerables } from 'chart.js';

if (typeof Chart !== 'undefined') {
  Chart.register(...registerables);
}

@Component({
  selector: 'app-financial-report',
  standalone: true,
  imports: [CommonModule, FormsModule, BaseChartDirective],
  templateUrl: './financial-report.component.html',
  styleUrl: './financial-report.component.scss'
})
export class FinancialReportComponent implements OnInit {
  report: FinancialReportSummary | null = null;
  loading = false;
  exporting = false;
  error: string | null = null;
  successMessage: string | null = null;

  startDate = '';
  endDate = '';

  public revenueMethodChartData: ChartConfiguration<'doughnut'>['data'] = {
    labels: ['Card', 'Payment Link', 'Cash'],
    datasets: [
      {
        data: [0, 0, 0],
        backgroundColor: ['#3B82F6', '#10B981', '#F59E0B'],
        borderWidth: 0
      }
    ]
  };
  public revenueMethodChartType: 'doughnut' = 'doughnut';
  public revenueMethodChartOptions: ChartOptions<'doughnut'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'bottom'
      }
    }
  };

  public statusChartData: ChartConfiguration<'bar'>['data'] = {
    labels: ['Pending', 'Under Review', 'Authorized', 'Paid', 'Failed', 'Refunded', 'Cancelled'],
    datasets: [
      {
        label: 'Payments',
        data: [0, 0, 0, 0, 0, 0, 0],
        backgroundColor: ['#F59E0B', '#F97316', '#6366F1', '#10B981', '#EF4444', '#8B5CF6', '#6B7280']
      }
    ]
  };
  public statusChartType: 'bar' = 'bar';
  public statusChartOptions: ChartOptions<'bar'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: false
      }
    },
    scales: {
      y: {
        beginAtZero: true,
        ticks: {
          precision: 0
        }
      },
      x: {
        grid: {
          display: false
        }
      }
    }
  };

  constructor(private reportsService: ReportsService) {}

  ngOnInit(): void {
    this.setDefaultDateRange();
    this.loadReport();
  }

  setDefaultDateRange(): void {
    const now = new Date();
    const firstDay = new Date(now.getFullYear(), now.getMonth(), 1);
    const lastDay = new Date(now.getFullYear(), now.getMonth() + 1, 0);

    this.startDate = this.formatDateForInput(firstDay);
    this.endDate = this.formatDateForInput(lastDay);
  }

  loadReport(): void {
    this.loading = true;
    this.error = null;
    this.successMessage = null;

    this.reportsService
      .getFinancialReport(this.startDate || undefined, this.endDate || undefined)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (response) => {
          this.report = response;
          this.updateCharts();
        },
        error: (err) => {
          this.report = null;
          this.error = err?.error?.message || 'Failed to load financial report.';
        }
      });
  }

  onFilterApply(): void {
    this.loadReport();
  }

  clearFilters(): void {
    this.startDate = '';
    this.endDate = '';
    this.loadReport();
  }

  exportCsv(): void {
    this.exporting = true;
    this.error = null;
    this.successMessage = null;

    this.reportsService
      .exportFinancialCsv(this.startDate || undefined, this.endDate || undefined)
      .pipe(finalize(() => (this.exporting = false)))
      .subscribe({
        next: (blob) => {
          const fileName = `financial-report-${new Date().toISOString().slice(0, 10)}.csv`;
          this.downloadBlob(blob, fileName);
          this.successMessage = 'Financial report exported successfully.';
        },
        error: (err) => {
          this.error = err?.error?.message || 'Failed to export CSV.';
        }
      });
  }

  formatCurrency(value: number | undefined): string {
    return (value || 0).toLocaleString('en-US', {
      style: 'currency',
      currency: 'USD'
    });
  }

  getRefundRate(): string {
    if (!this.report || this.report.totalRevenue <= 0) {
      return '0.0%';
    }

    return `${((this.report.totalRefunded / this.report.totalRevenue) * 100).toFixed(1)}%`;
  }

  getCollectionRate(): string {
    if (!this.report || this.report.totalPayments <= 0) {
      return '0.0%';
    }

    const collected = this.report.statusCounts.paid + this.report.statusCounts.partiallyRefunded;
    return `${((collected / this.report.totalPayments) * 100).toFixed(1)}%`;
  }

  getAvgRevenuePerPayment(): string {
    if (!this.report || this.report.totalPayments <= 0) {
      return this.formatCurrency(0);
    }

    return this.formatCurrency(this.report.netRevenue / this.report.totalPayments);
  }

  private updateCharts(): void {
    if (!this.report) {
      return;
    }

    this.revenueMethodChartData = {
      ...this.revenueMethodChartData,
      datasets: [
        {
          ...this.revenueMethodChartData.datasets[0],
          data: [
            this.report.revenueByMethod.card || 0,
            this.report.revenueByMethod.paymentLink || 0,
            this.report.revenueByMethod.cash || 0
          ]
        }
      ]
    };

    this.statusChartData = {
      ...this.statusChartData,
      datasets: [
        {
          ...this.statusChartData.datasets[0],
          data: [
            this.report.statusCounts.pending || 0,
            this.report.statusCounts.underReview || 0,
            this.report.statusCounts.authorized || 0,
            this.report.statusCounts.paid || 0,
            this.report.statusCounts.failed || 0,
            this.report.statusCounts.refunded || 0,
            this.report.statusCounts.cancelled || 0
          ]
        }
      ]
    };
  }

  private formatDateForInput(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private downloadBlob(blob: Blob, fileName: string): void {
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    a.click();
    window.URL.revokeObjectURL(url);
  }
}
