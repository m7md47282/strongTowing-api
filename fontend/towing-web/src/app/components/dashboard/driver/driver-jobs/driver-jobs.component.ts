import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { take } from 'rxjs';
import {
  JobService,
  Job,
  JOB_STATUS,
  JobStatus
} from '../../../../services/job.service';

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
    { value: JOB_STATUS.Waiting, label: 'Waiting' },
    { value: JOB_STATUS.Dispatch, label: 'Dispatch' },
    { value: JOB_STATUS.OnRoute, label: 'On route' },
    { value: JOB_STATUS.OnScene, label: 'On scene' },
    { value: JOB_STATUS.Loaded, label: 'Loaded' },
    { value: JOB_STATUS.Completed, label: 'Completed' },
    { value: JOB_STATUS.Cancelled, label: 'Cancelled' }
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
    const map: Record<JobStatus, string> = {
      [JOB_STATUS.Waiting]: 'bg-gray-100 text-gray-800',
      [JOB_STATUS.Dispatch]: 'bg-blue-100 text-blue-800',
      [JOB_STATUS.OnRoute]: 'bg-indigo-100 text-indigo-800',
      [JOB_STATUS.OnScene]: 'bg-amber-100 text-amber-900',
      [JOB_STATUS.Loaded]: 'bg-purple-100 text-purple-800',
      [JOB_STATUS.Completed]: 'bg-green-100 text-green-800',
      [JOB_STATUS.Cancelled]: 'bg-red-100 text-red-800'
    };
    return map[status as JobStatus] || 'bg-gray-100 text-gray-800';
  }

  formatStatus(status: string): string {
    const map: Record<JobStatus, string> = {
      [JOB_STATUS.Waiting]: 'Waiting',
      [JOB_STATUS.Dispatch]: 'Dispatch',
      [JOB_STATUS.OnRoute]: 'On route',
      [JOB_STATUS.OnScene]: 'On scene',
      [JOB_STATUS.Loaded]: 'Loaded',
      [JOB_STATUS.Completed]: 'Completed',
      [JOB_STATUS.Cancelled]: 'Cancelled'
    };
    return map[status as JobStatus] || status;
  }

  vehicleLabel(job: Job): string {
    const v = job.vehicle;
    if (!v) {
      return '—';
    }
    return [v.year, v.make, v.model].filter(Boolean).join(' ') || '—';
  }
}
