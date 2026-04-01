import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { JobService, Job } from '../../../../services/job.service';

@Component({
  selector: 'app-driver-home',
  standalone: true,
  imports: [CommonModule, RouterModule],
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
    const closed = new Set(['Completed', 'Cancelled']);
    this.completedCount = jobs.filter((j) => j.status === 'Completed').length;
    this.activeCount = jobs.filter((j) => !closed.has(j.status)).length;
  }
}
