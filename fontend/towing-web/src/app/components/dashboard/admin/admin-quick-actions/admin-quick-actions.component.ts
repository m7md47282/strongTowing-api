import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Job } from '../../../../services/job.service';

@Component({
  selector: 'app-admin-quick-actions',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './admin-quick-actions.component.html',
  styleUrl: './admin-quick-actions.component.scss'
})
export class AdminQuickActionsComponent {
  @Input() recentActiveJobs: Job[] = [];

  jobSearchQuery = '';

  readonly quickLinks: ReadonlyArray<{
    label: string;
    description: string;
    route: string;
    icon: string;
  }> = [
    { label: 'Jobs', description: 'Dispatch & status', route: '/admin/jobs', icon: 'fas fa-tasks' },
    { label: 'Drivers', description: 'Availability & assignments', route: '/admin/drivers', icon: 'fas fa-id-card' },
    { label: 'Invoices', description: 'Billing & PDFs', route: '/admin/invoices', icon: 'fas fa-file-invoice-dollar' },
    { label: 'Payments', description: 'Revenue & methods', route: '/admin/payments', icon: 'fas fa-credit-card' },
    { label: 'Reports', description: 'Financial summary', route: '/admin/reports/financial', icon: 'fas fa-chart-line' },
    { label: 'Settings', description: 'Company defaults', route: '/admin/settings', icon: 'fas fa-cog' }
  ];

  constructor(private router: Router) {}

  searchJobs(): void {
    const q = this.jobSearchQuery.trim();
    this.router.navigate(['/admin', 'jobs'], {
      queryParams: q ? { search: q } : {}
    });
  }
}
