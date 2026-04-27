import { Component, OnDestroy, OnInit } from '@angular/core';
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

interface DriversPagedResponse extends PagedResponse<User> {
  driversWithActiveJobCount: number;
  driversWithoutActiveJobCount: number;
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
export class DriverAssignmentsComponent implements OnInit, OnDestroy {
  drivers: DriverWithJob[] = [];
  loading = false;
  error: string | null = null;
  searchTerm = '';
  assignmentFilter: AssignmentFilter = '';

  pageNumber = 1;
  pageSize = 25;
  totalCount = 0;
  totalPages = 0;
  hasPreviousPage = false;
  hasNextPage = false;
  Math = Math;

  stats = {
    eligibleTotal: 0,
    assigned: 0,
    unassigned: 0
  };

  private searchDebounceTimer: ReturnType<typeof setTimeout> | undefined;

  private readonly activeStatuses: JobStatus[] = [...JOB_STATUS_ACTIVE];

  constructor(
    private apiService: ApiService,
    private jobService: JobService
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  ngOnDestroy(): void {
    clearTimeout(this.searchDebounceTimer);
  }

  private buildDriverListParams(): HttpParams {
    let params = new HttpParams()
      .set('pageNumber', String(this.pageNumber))
      .set('pageSize', String(this.pageSize))
      .set('isActive', 'true')
      .set('availableForDispatchOnly', 'true');
    const t = this.searchTerm.trim();
    if (t) {
      params = params.set('search', t);
    }
    if (this.assignmentFilter) {
      params = params.set('assignment', this.assignmentFilter);
    }
    return params;
  }

  loadData(): void {
    this.loading = true;
    this.error = null;

    forkJoin({
      drivers: this.apiService
        .get<DriversPagedResponse>('users/drivers', this.buildDriverListParams())
        .pipe(
          catchError((err) => {
            this.error = err.error?.message || err.error?.error || 'Failed to load drivers';
            return of({
              data: [] as User[],
              pageNumber: 1,
              pageSize: this.pageSize,
              totalCount: 0,
              totalPages: 0,
              hasPreviousPage: false,
              hasNextPage: false,
              driversWithActiveJobCount: 0,
              driversWithoutActiveJobCount: 0
            } as DriversPagedResponse);
          })
        ),
      jobs: this.jobService.getAllJobs().pipe(
        catchError((err) => {
          console.error('Failed to load jobs for driver view:', err);
          return of([] as Job[]);
        })
      )
    })
      .pipe(finalize(() => {
        this.loading = false;
      }))
      .subscribe(({ drivers, jobs }) => {
        const activeJobs = jobs.filter((job) => this.activeStatuses.includes(job.status));
        this.pageNumber = drivers.pageNumber;
        this.pageSize = drivers.pageSize;
        this.totalCount = drivers.totalCount;
        this.totalPages = drivers.totalPages;
        this.hasPreviousPage = drivers.hasPreviousPage;
        this.hasNextPage = drivers.hasNextPage;

        this.stats.eligibleTotal =
          (drivers.driversWithActiveJobCount ?? 0) +
          (drivers.driversWithoutActiveJobCount ?? 0);
        this.stats.assigned = drivers.driversWithActiveJobCount ?? 0;
        this.stats.unassigned = drivers.driversWithoutActiveJobCount ?? 0;

        this.drivers = (drivers.data || []).map((driver) => ({
          ...driver,
          currentJob: this.findCurrentJob(driver, activeJobs)
        }));
      });
  }

  findCurrentJob(driver: User, jobs: Job[]): Job | null {
    const driverJobs = jobs.filter((job) => job.driverId === driver.id);
    if (driverJobs.length === 0) {
      return null;
    }

    return driverJobs.sort(
      (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    )[0];
  }

  onSearchInput(): void {
    clearTimeout(this.searchDebounceTimer);
    this.searchDebounceTimer = setTimeout(() => {
      this.pageNumber = 1;
      this.loadData();
    }, 400);
  }

  submitSearchNow(): void {
    clearTimeout(this.searchDebounceTimer);
    this.pageNumber = 1;
    this.loadData();
  }

  onAssignmentFilterChange(): void {
    this.pageNumber = 1;
    this.loadData();
  }

  onPageSizeChange(): void {
    this.pageNumber = 1;
    this.loadData();
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.pageNumber = page;
      this.loadData();
    }
  }

  nextPage(): void {
    if (this.hasNextPage) {
      this.pageNumber++;
      this.loadData();
    }
  }

  previousPage(): void {
    if (this.hasPreviousPage) {
      this.pageNumber--;
      this.loadData();
    }
  }

  getPageNumbers(): number[] {
    const pages: number[] = [];
    const maxPagesToShow = 5;
    let startPage = Math.max(1, this.pageNumber - Math.floor(maxPagesToShow / 2));
    let endPage = Math.min(this.totalPages, startPage + maxPagesToShow - 1);
    if (endPage - startPage < maxPagesToShow - 1) {
      startPage = Math.max(1, endPage - maxPagesToShow + 1);
    }
    for (let i = startPage; i <= endPage; i++) {
      pages.push(i);
    }
    return pages;
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.assignmentFilter = '';
    this.pageNumber = 1;
    this.loadData();
  }

  hasActiveFilters(): boolean {
    return !!(this.searchTerm.trim() || this.assignmentFilter);
  }

  refresh(): void {
    this.loadData();
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
