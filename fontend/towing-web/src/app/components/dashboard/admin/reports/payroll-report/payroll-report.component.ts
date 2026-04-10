import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { ReportsService, DriverPayrollAdminRow } from '../../../../../services/reports.service';
import { SettingsService } from '../../../../../services/settings.service';

@Component({
  selector: 'app-payroll-report',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './payroll-report.component.html',
  styleUrl: './payroll-report.component.scss'
})
export class PayrollReportComponent implements OnInit {
  rows: DriverPayrollAdminRow[] = [];
  loading = false;
  exporting = false;
  generating = false;
  error: string | null = null;
  successMessage: string | null = null;

  /** List / export filter */
  filterStartDate = '';
  filterEndDate = '';
  statusFilter = '';

  /** Generate draft snapshots for this inclusive period */
  genStartDate = '';
  genEndDate = '';

  actionLoadingId: number | null = null;

  /** Current driver commission % from Settings (for generate-draft copy). */
  settingsDriverCommissionPct: number | null = null;

  constructor(
    private reportsService: ReportsService,
    private settingsService: SettingsService
  ) {}

  ngOnInit(): void {
    this.setDefaultDateRange();
    this.loadList();
    this.settingsService.getSettings().subscribe({
      next: (s) => (this.settingsDriverCommissionPct = s.driverCommissionPercentage),
      error: () => (this.settingsDriverCommissionPct = null)
    });
  }

  setDefaultDateRange(): void {
    const now = new Date();
    const firstDay = new Date(now.getFullYear(), now.getMonth(), 1);
    const lastDay = new Date(now.getFullYear(), now.getMonth() + 1, 0);
    const s = this.formatDateForInput(firstDay);
    const e = this.formatDateForInput(lastDay);
    this.filterStartDate = s;
    this.filterEndDate = e;
    this.genStartDate = s;
    this.genEndDate = e;
  }

  loadList(): void {
    this.loading = true;
    this.error = null;
    this.successMessage = null;

    this.reportsService
      .getPayrollList(
        this.filterStartDate || undefined,
        this.filterEndDate || undefined,
        this.statusFilter || undefined
      )
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (rows) => (this.rows = rows),
        error: (err) => {
          this.rows = [];
          this.error = this.formatApiError(err, 'Failed to load payroll report.');
        }
      });
  }

  onFilterApply(): void {
    this.loadList();
  }

  clearFilters(): void {
    this.filterStartDate = '';
    this.filterEndDate = '';
    this.statusFilter = '';
    this.loadList();
  }

  generatePayroll(): void {
    if (!this.genStartDate || !this.genEndDate) {
      this.error = 'Select a pay period start and end for generation.';
      return;
    }

    this.generating = true;
    this.error = null;
    this.successMessage = null;

    this.reportsService
      .generateDriverPayroll(this.genStartDate, this.genEndDate)
      .pipe(finalize(() => (this.generating = false)))
      .subscribe({
        next: (res) => {
          this.successMessage = `Generated or refreshed ${res.rowsUpserted} draft row(s) for this period.`;
          this.rows = res.rows;
          this.filterStartDate = res.payPeriodStart.slice(0, 10);
          this.filterEndDate = res.payPeriodEnd.slice(0, 10);
        },
        error: (err) => {
          this.error = this.formatApiError(err, 'Failed to generate payroll.');
        }
      });
  }

  finalize(row: DriverPayrollAdminRow): void {
    this.runRowAction(row.id, this.reportsService.finalizePayroll(row.id), 'Payroll finalized.');
  }

  markPaid(row: DriverPayrollAdminRow): void {
    this.runRowAction(row.id, this.reportsService.markPayrollPaid(row.id), 'Marked as paid.');
  }

  private runRowAction(id: number, obs: Observable<DriverPayrollAdminRow>, okMsg: string): void {
    this.actionLoadingId = id;
    this.error = null;
    this.successMessage = null;
    obs.pipe(finalize(() => (this.actionLoadingId = null))).subscribe({
      next: () => {
        this.successMessage = okMsg;
        this.loadList();
      },
      error: (err) => {
        this.error = this.formatApiError(err, 'Action failed.');
      }
    });
  }

  exportCsv(): void {
    this.exporting = true;
    this.error = null;
    this.reportsService
      .exportPayrollCsv(
        this.filterStartDate || undefined,
        this.filterEndDate || undefined,
        this.statusFilter || undefined
      )
      .pipe(finalize(() => (this.exporting = false)))
      .subscribe({
        next: (blob) => {
          const fileName = `driver-payroll-${new Date().toISOString().slice(0, 10)}.csv`;
          this.downloadBlob(blob, fileName);
          this.successMessage = 'Payroll exported successfully.';
        },
        error: (err) => {
          this.error = this.formatApiError(err, 'Failed to export CSV.');
        }
      });
  }

  /** Maps payroll API error JSON (message, detail, inner, correlationId) for display. */
  private formatApiError(err: unknown, fallback: string): string {
    const body = (err as { error?: Record<string, unknown> })?.error;
    if (!body || typeof body !== 'object') {
      return fallback;
    }
    const msg = typeof body['message'] === 'string' ? (body['message'] as string) : fallback;
    const detail = typeof body['detail'] === 'string' ? (body['detail'] as string) : '';
    const inner = typeof body['inner'] === 'string' ? (body['inner'] as string) : '';
    const ref = typeof body['correlationId'] === 'string' ? (body['correlationId'] as string) : '';
    let out = msg;
    if (detail) {
      out += `\n\n${detail}`;
    }
    if (inner) {
      out += `\n\nInner: ${inner}`;
    }
    if (ref) {
      out += `\n\nReference: ${ref}`;
    }
    return out.trim() || fallback;
  }

  formatCurrency(value: number | undefined): string {
    return (value ?? 0).toLocaleString('en-US', {
      style: 'currency',
      currency: 'USD'
    });
  }

  formatPeriod(iso: string): string {
    if (!iso) {
      return '';
    }
    const d = new Date(iso);
    return d.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  formatJobTimeMinutes(total: number): string {
    if (total <= 0) {
      return '—';
    }
    const h = Math.floor(total / 60);
    const m = total % 60;
    if (h <= 0) {
      return `${m}m`;
    }
    return `${h}h ${m}m`;
  }

  totals(): { gross: number; net: number; cash: number; jobs: number; revenue: number } {
    return this.rows.reduce(
      (acc, r) => ({
        gross: acc.gross + (r.grossEarnings ?? 0),
        net: acc.net + (r.netPay ?? 0),
        cash: acc.cash + (r.cashCollections ?? 0),
        jobs: acc.jobs + (r.totalJobs ?? 0),
        revenue: acc.revenue + (r.totalJobRevenue ?? 0)
      }),
      { gross: 0, net: 0, cash: 0, jobs: 0, revenue: 0 }
    );
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
