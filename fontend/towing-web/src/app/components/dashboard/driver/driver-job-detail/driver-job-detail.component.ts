import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import {
  JobService,
  Job,
  UpdateJobStatusRequest,
  resolvePublicAssetUrl,
  JOB_STATUS,
  JOB_STATUS_PIPELINE,
  JobStatus
} from '../../../../services/job.service';
import { SettingsService, DispatchContact } from '../../../../services/settings.service';
import { LocationService } from '../../../../services/location.service';
import { parseApiError } from '../../../../utils/api-error.util';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'app-driver-job-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './driver-job-detail.component.html',
  styleUrl: './driver-job-detail.component.scss'
})
export class DriverJobDetailComponent implements OnInit {
  loading = true;
  submitting = false;
  uploading = false;
  loadError: string | null = null;
  statusError: string | null = null;
  uploadError: string | null = null;
  job: Job | null = null;
  dispatchContact: DispatchContact | null = null;
  locationPingMessage: string | null = null;
  locationPinging = false;

  readonly statusProgression: JobStatus[] = [...JOB_STATUS_PIPELINE];

  /** Minimum photos required before driver can advance to Loaded (matches API). */
  readonly minPhotosRequiredForLoaded = 10;

  updateStatusControl = new FormControl<string>('', { nonNullable: true });

  readonly resolvePhotoUrl = resolvePublicAssetUrl;

  constructor(
    private jobService: JobService,
    private route: ActivatedRoute,
    private router: Router,
    private settings: SettingsService,
    private location: LocationService
  ) {}

  ngOnInit(): void {
    this.settings.getDispatchContact().subscribe({
      next: (c) => (this.dispatchContact = c),
      error: () => (this.dispatchContact = null)
    });

    this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      if (!id) {
        void this.router.navigate(['/driver/jobs']);
        return;
      }
      this.loadJob(+id);
    });
  }

  loadJob(id: number): void {
    this.loading = true;
    this.loadError = null;
    this.statusError = null;
    this.uploadError = null;
    this.job = null;
    this.jobService
      .getJobById(id)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (job) => {
          this.job = job;
          const next = this.getNextStatuses(job);
          this.updateStatusControl.setValue(next[0] ?? '');
        },
        error: (err) => {
          if (err.status === 403) {
            this.loadError = 'You do not have access to this job.';
          } else {
            this.loadError = parseApiError(err) || 'Could not load job.';
          }
        }
      });
  }

  getNextStatuses(job: Job): JobStatus[] {
    const currentIndex = this.statusProgression.indexOf(job.status);
    if (currentIndex === -1) {
      return [];
    }
    return this.statusProgression.slice(currentIndex + 1);
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

  mapsUrl(address: string | null | undefined): string {
    const q = (address || '').trim();
    if (!q) return '';
    return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(q)}`;
  }

  telHref(phone: string | null | undefined): string {
    const digits = (phone || '').replace(/\D/g, '');
    return digits ? `tel:${digits}` : '';
  }

  canUploadPhoto(job: Job): boolean {
    return (job.photoCount ?? job.photos?.length ?? 0) < this.minPhotosRequiredForLoaded;
  }

  onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file || !this.job) {
      return;
    }

    this.uploading = true;
    this.uploadError = null;
    this.jobService
      .uploadJobPhoto(this.job.id, file)
      .pipe(finalize(() => (this.uploading = false)))
      .subscribe({
        next: (updated) => {
          this.job = updated;
        },
        error: (err) => {
          this.uploadError = parseApiError(err) || 'Upload failed.';
        }
      });
  }

  shareLocationWithDispatch(): void {
    if (!navigator.geolocation) {
      this.locationPingMessage = 'Location is not supported in this browser.';
      return;
    }
    this.locationPinging = true;
    this.locationPingMessage = null;
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        this.location
          .pingDriverLocation(pos.coords.latitude, pos.coords.longitude)
          .pipe(finalize(() => (this.locationPinging = false)))
          .subscribe({
            next: () => {
              this.locationPingMessage = 'Location sent to dispatch.';
            },
            error: (err) => {
              this.locationPingMessage = parseApiError(err);
            }
          });
      },
      (geoErr) => {
        this.locationPinging = false;
        this.locationPingMessage = geoErr?.message || 'Could not read GPS.';
      },
      { enableHighAccuracy: true, timeout: 20000, maximumAge: 0 }
    );
  }

  onUpdateStatus(): void {
    if (!this.job || this.updateStatusControl.invalid) {
      this.updateStatusControl.markAsTouched();
      return;
    }
    const next = this.getNextStatuses(this.job);
    const newStatus = this.updateStatusControl.value as UpdateJobStatusRequest['status'];
    if (!next.includes(newStatus)) {
      this.statusError = 'Invalid status for this job.';
      return;
    }

    this.submitting = true;
    this.statusError = null;
    this.jobService
      .updateJobStatus(this.job.id, { status: newStatus })
      .pipe(
        finalize(() => (this.submitting = false)),
        catchError((err) => {
          this.statusError = parseApiError(err) || 'Failed to update status.';
          return of(null);
        })
      )
      .subscribe((result) => {
        if (result) {
          this.loadJob(this.job!.id);
        }
      });
  }
}
