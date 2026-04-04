import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs/operators';
import { ServicePricingService } from '../../../../services/service-pricing.service';
import {
  CreateServicePricingProfilePayload,
  ServicePricingProfile
} from '../../../../models/service-pricing.model';

@Component({
  selector: 'app-admin-services',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './services.component.html',
  styleUrl: './services.component.scss'
})
export class ServicesComponent implements OnInit {
  readonly serviceTypes = [
    'Towing',
    'Roadside Assistance',
    'Jump Start',
    'Tire Change',
    'Lockout',
    'Fuel Delivery',
    'Winch',
    'Recovery',
    'Other'
  ];

  profiles: ServicePricingProfile[] = [];
  filteredProfiles: ServicePricingProfile[] = [];
  loading = false;
  submitting = false;
  error: string | null = null;
  successMessage: string | null = null;
  includeUnavailable = false;
  searchTerm = '';

  showModal = false;
  editingId: number | null = null;
  form: FormGroup;

  deleteConfirmId: number | null = null;

  constructor(
    private servicePricingService: ServicePricingService,
    private fb: FormBuilder
  ) {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      loadedPrice: [0, [Validators.required, Validators.min(0)]],
      deadHeadPrice: [0, [Validators.required, Validators.min(0)]],
      isAvailable: [true]
    });
  }

  ngOnInit(): void {
    this.loadProfiles();
  }

  loadProfiles(): void {
    this.loading = true;
    this.error = null;

    this.servicePricingService
      .getAll(this.includeUnavailable)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (rows) => {
          this.profiles = rows;
          this.applyFilter();
        },
        error: (err: HttpErrorResponse) => {
          this.error = this.httpErrorMessage(err);
        }
      });
  }

  onIncludeUnavailableChange(): void {
    this.loadProfiles();
  }

  onSearchChange(): void {
    this.applyFilter();
  }

  private applyFilter(): void {
    const q = this.searchTerm.trim().toLowerCase();
    if (!q) {
      this.filteredProfiles = [...this.profiles];
      return;
    }

    this.filteredProfiles = this.profiles.filter((p) => p.name.toLowerCase().includes(q));
  }

  openCreateModal(): void {
    this.editingId = null;
    this.form.reset({
      name: '',
      loadedPrice: 0,
      deadHeadPrice: 0,
      isAvailable: true
    });
    this.error = null;
    this.showModal = true;
  }

  openEditModal(profile: ServicePricingProfile): void {
    this.editingId = profile.id;
    this.form.patchValue({
      name: profile.name,
      loadedPrice: profile.loadedPrice,
      deadHeadPrice: profile.deadHeadPrice,
      isAvailable: profile.isAvailable
    });
    this.error = null;
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.editingId = null;
  }

  saveProfile(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const payload: CreateServicePricingProfilePayload = {
      name: String(raw.name || '').trim(),
      loadedPrice: Number(raw.loadedPrice ?? 0),
      deadHeadPrice: Number(raw.deadHeadPrice ?? 0),
      isAvailable: Boolean(raw.isAvailable)
    };

    this.submitting = true;
    this.error = null;

    const req = this.editingId == null
      ? this.servicePricingService.create(payload)
      : this.servicePricingService.update(this.editingId, payload);

    req.pipe(finalize(() => (this.submitting = false))).subscribe({
      next: () => {
        this.successMessage = this.editingId == null
          ? 'Service pricing profile created.'
          : 'Service pricing profile updated.';
        this.closeModal();
        this.loadProfiles();
        setTimeout(() => (this.successMessage = null), 4000);
      },
      error: (err: HttpErrorResponse) => {
        this.error = this.httpErrorMessage(err);
      }
    });
  }

  confirmDelete(profile: ServicePricingProfile): void {
    this.deleteConfirmId = profile.id;
  }

  setAvailability(profile: ServicePricingProfile, isAvailable: boolean): void {
    if (profile.isAvailable === isAvailable) {
      return;
    }

    this.submitting = true;
    this.error = null;
    this.servicePricingService
      .setAvailability(profile.id, isAvailable)
      .pipe(finalize(() => (this.submitting = false)))
      .subscribe({
        next: () => {
          this.successMessage = isAvailable ? 'Service marked available.' : 'Service marked unavailable.';
          this.loadProfiles();
          setTimeout(() => (this.successMessage = null), 4000);
        },
        error: (err: HttpErrorResponse) => {
          this.error = this.httpErrorMessage(err);
        }
      });
  }

  cancelDelete(): void {
    this.deleteConfirmId = null;
  }

  executeDelete(): void {
    if (this.deleteConfirmId == null) {
      return;
    }

    const id = this.deleteConfirmId;
    this.submitting = true;
    this.error = null;

    this.servicePricingService
      .delete(id)
      .pipe(finalize(() => (this.submitting = false)))
      .subscribe({
        next: () => {
          this.deleteConfirmId = null;
          this.successMessage = 'Service pricing profile deleted.';
          this.loadProfiles();
          setTimeout(() => (this.successMessage = null), 4000);
        },
        error: (err: HttpErrorResponse) => {
          this.error = this.httpErrorMessage(err);
        }
      });
  }

  formatDate(dateString: string): string {
    if (!dateString) {
      return '—';
    }
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  private httpErrorMessage(err: HttpErrorResponse): string {
    const e = err.error;
    if (e && typeof e === 'object' && 'message' in e && typeof (e as { message?: string }).message === 'string') {
      return (e as { message: string }).message;
    }
    if (typeof e === 'string' && e.length > 0) {
      return e;
    }
    return err.message || 'Request failed.';
  }
}
