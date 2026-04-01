import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { take } from 'rxjs';
import { JobService, Job } from '../../../../services/job.service';

@Component({
  selector: 'app-driver-jobs',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './driver-jobs.component.html',
  styleUrl: './driver-jobs.component.scss'
})
export class DriverJobsComponent implements OnInit {
  loading = true;
  error: string | null = null;
  jobs: Job[] = [];
  statusFilter = '';

  statusOptions = [
    { value: '', label: 'All statuses' },
    { value: 'Assigned', label: 'Assigned' },
    { value: 'OnRoute', label: 'On route' },
    { value: 'InProgress', label: 'In progress' },
    { value: 'ReadyToRelease', label: 'Ready to release' },
    { value: 'Completed', label: 'Completed' }
  ];

  constructor(
    private jobService: JobService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.route.queryParams.pipe(take(1)).subscribe((params) => {
      const jobId = params['jobId'];
      if (jobId) {
        void this.router.navigate(['/driver/jobs', jobId], { replaceUrl: true });
        return;
      }
      this.loadJobs();
    });
  }

  loadJobs(): void {
    this.loading = true;
    this.error = null;
    this.jobService
      .getMyJobs(this.statusFilter || undefined)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (jobs) => {
          this.jobs = jobs;
        },
        error: (err) => {
          this.error = err.error?.message || 'Could not load your jobs.';
        }
      });
  }

  onFilterChange(): void {
    this.loadJobs();
  }

  getStatusClass(status: string): string {
    const map: Record<string, string> = {
      Pending: 'bg-gray-100 text-gray-800',
      Assigned: 'bg-blue-100 text-blue-800',
      OnRoute: 'bg-indigo-100 text-indigo-800',
      InProgress: 'bg-amber-100 text-amber-900',
      ReadyToRelease: 'bg-purple-100 text-purple-800',
      Completed: 'bg-green-100 text-green-800',
      Cancelled: 'bg-red-100 text-red-800'
    };
    return map[status] || 'bg-gray-100 text-gray-800';
  }

  formatStatus(status: string): string {
    const map: Record<string, string> = {
      OnRoute: 'On route',
      InProgress: 'In progress',
      ReadyToRelease: 'Ready to release'
    };
    return map[status] || status;
  }

  vehicleLabel(job: Job): string {
    const v = job.vehicle;
    if (!v) {
      return '—';
    }
    return [v.year, v.make, v.model].filter(Boolean).join(' ') || '—';
  }
}
