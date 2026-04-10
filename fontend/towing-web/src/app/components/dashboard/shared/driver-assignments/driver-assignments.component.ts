import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpParams } from '@angular/common/http';
import { forkJoin, of } from 'rxjs';
import { catchError, finalize } from 'rxjs/operators';
import { ApiService } from '../../../../services/api.service';
import {
  JobService,
  Job,
  JOB_STATUS,
  JOB_STATUS_ACTIVE,
  JobStatus
} from '../../../../services/job.service';
import { User } from '../../../../models/user.model';

interface PagedResponse<T> {
  data: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

type AssignmentFilter = 'assigned' | 'unassigned' | '';

interface DriverWithJob extends User {
  currentJob: Job | null;
}

@Component({
  selector: 'app-driver-assignments',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './driver-assignments.component.html',
  styleUrls: ['./driver-assignments.component.scss']
})
export class DriverAssignmentsComponent implements OnInit {
  drivers: DriverWithJob[] = [];
  filteredDrivers: DriverWithJob[] = [];
  loading = false;
  error: string | null = null;
  searchTerm = '';
  assignmentFilter: AssignmentFilter = '';

  stats = {
    total: 0,
    assigned: 0,
    unassigned: 0
  };

  private readonly activeStatuses: JobStatus[] = [...JOB_STATUS_ACTIVE];

  constructor(
    private apiService: ApiService,
    private jobService: JobService
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    this.error = null;

    const params = new HttpParams()
      .set('pageNumber', '1')
      .set('pageSize', '100')
      .set('isActive', 'true')
      .set('availableForDispatchOnly', 'true');

    forkJoin({
      drivers: this.apiService.get<PagedResponse<User>>('users/drivers', params).pipe(
        catchError(err => {
          this.error = err.error?.message || err.error?.error || 'Failed to load drivers';
          return of({
            data: [],
            pageNumber: 1,
            pageSize: 100,
            totalCount: 0,
            totalPages: 0,
            hasPreviousPage: false,
            hasNextPage: false
          } as PagedResponse<User>);
        })
      ),
      jobs: this.jobService.getAllJobs().pipe(
        catchError(err => {
          console.error('Failed to load jobs for driver view:', err);
          return of([] as Job[]);
        })
      )
    }).pipe(finalize(() => {
      this.loading = false;
    }))
    .subscribe(({ drivers, jobs }) => {
      const activeJobs = jobs.filter(job => this.activeStatuses.includes(job.status));
      this.drivers = drivers.data.map(driver => ({
        ...driver,
        currentJob: this.findCurrentJob(driver, activeJobs)
      }));

      this.computeStats();
      this.applyFilters();
    });
  }

  findCurrentJob(driver: User, jobs: Job[]): Job | null {
    const driverJobs = jobs.filter(job => job.driverId === driver.id);
    if (driverJobs.length === 0) {
      return null;
    }

    return driverJobs.sort((a, b) =>
      new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    )[0];
  }

  applyFilters(): void {
    this.filteredDrivers = this.drivers.filter(driver => {
      const matchesSearch = this.matchesSearch(driver);
      const matchesAssignment = this.matchesAssignment(driver);
      return matchesSearch && matchesAssignment;
    });
  }

  private matchesSearch(driver: DriverWithJob): boolean {
    if (!this.searchTerm.trim()) return true;
    const term = this.searchTerm.toLowerCase();
    return (
      driver.fullName.toLowerCase().includes(term) ||
      driver.email.toLowerCase().includes(term) ||
      (driver.phoneNumber || '').toLowerCase().includes(term)
    );
  }

  private matchesAssignment(driver: DriverWithJob): boolean {
    if (!this.assignmentFilter) return true;
    if (this.assignmentFilter === 'assigned') {
      return !!driver.currentJob;
    }
    return !driver.currentJob;
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.assignmentFilter = '';
    this.applyFilters();
  }

  refresh(): void {
    this.loadData();
  }

  computeStats(): void {
    this.stats.total = this.drivers.length;
    this.stats.assigned = this.drivers.filter(d => d.currentJob).length;
    this.stats.unassigned = this.stats.total - this.stats.assigned;
  }

  getJobBadgeClass(status?: JobStatus): string {
    switch (status) {
      case JOB_STATUS.Waiting:
        return 'bg-yellow-100 text-yellow-800';
      case JOB_STATUS.Dispatch:
        return 'bg-blue-100 text-blue-800';
      case JOB_STATUS.OnRoute:
        return 'bg-indigo-100 text-indigo-800';
      case JOB_STATUS.OnScene:
        return 'bg-purple-100 text-purple-800';
      case JOB_STATUS.Loaded:
        return 'bg-teal-100 text-teal-800';
      case JOB_STATUS.Completed:
        return 'bg-green-100 text-green-800';
      case JOB_STATUS.Cancelled:
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }
}

