import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { JobService, Job, JOB_STATUS, JobStatus } from '../../../../services/job.service';
import { DashboardPageSkeletonComponent } from '../../shared/dashboard-page-skeleton/dashboard-page-skeleton.component';

@Component({
  selector: 'app-driver-home',
  standalone: true,
  imports: [CommonModule, RouterModule, DashboardPageSkeletonComponent],
  templateUrl: './driver-home.component.html',
  styleUrl: './driver-home.component.scss'
})
export class DriverHomeComponent implements OnInit {
  loading = true;
  error: string | null = null;
  activeCount = 0;
  completedCount = 0;

  constructor(private jobService: JobService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = null;
    this.jobService
      .getMyJobs()
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (jobs) => this.applyCounts(jobs),
        error: () => {
          this.error = 'Could not load your jobs.';
        }
      });
  }

  private applyCounts(jobs: Job[]): void {
    const closed = new Set<JobStatus>([JOB_STATUS.Completed, JOB_STATUS.Cancelled]);
    this.completedCount = jobs.filter((j) => j.status === JOB_STATUS.Completed).length;
    this.activeCount = jobs.filter((j) => !closed.has(j.status)).length;
  }
}
