import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { catchError, finalize } from 'rxjs/operators';
import { of } from 'rxjs';
import {
  TruckService,
  TruckListItem,
  CreateTruckRequest,
  UpdateTruckRequest
} from '../../../../services/truck.service';
import { TruckTypeService, TruckType, CreateTruckTypeRequest } from '../../../../services/truck-type.service';
import { AuthService } from '../../../../services/auth.service';
import { RoleId } from '../../../../constants/user-roles.constants';

@Component({
  selector: 'app-trucks',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule],
  templateUrl: './trucks.component.html',
  styleUrls: ['./trucks.component.scss']
})
export class TrucksComponent implements OnInit {
  trucks: TruckListItem[] = [];
  truckTypes: TruckType[] = [];
  loading = false;
  typesLoading = false;
  error: string | null = null;

  activePanel: 'fleet' | 'types' = 'fleet';

  showTypeModal = false;
  editingType: TruckType | null = null;
  typeForm: FormGroup;
  typeSubmitting = false;

  showTruckModal = false;
  editingTruck: TruckListItem | null = null;
  truckForm: FormGroup;
  truckSubmitting = false;

  constructor(
    private truckService: TruckService,
    private truckTypeService: TruckTypeService,
    private fb: FormBuilder,
    private authService: AuthService
  ) {
    this.typeForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(120)]],
      description: ['']
    });
    this.truckForm = this.fb.group({
      truckTypeId: [null as number | null, Validators.required],
      unitLabel: ['', [Validators.required, Validators.maxLength(120)]],
      licensePlate: [''],
      vin: [''],
      make: [''],
      model: [''],
      year: [null as number | null],
      notes: [''],
      isOutOfService: [false],
      isActive: [true]
    });
  }

  ngOnInit(): void {
    this.loadAll();
  }

  jobListBasePath(): string {
    const u = this.authService.getCurrentUser();
    const r = Number(u?.roleId);
    if (r === RoleId.Dispatcher) {
      return '/dispatcher/jobs';
    }
    return '/admin/jobs';
  }

  loadAll(): void {
    this.loadTruckTypes();
    this.loadTrucks();
  }

  loadTruckTypes(): void {
    this.typesLoading = true;
    this.truckTypeService
      .getAll()
      .pipe(
        finalize(() => {
          this.typesLoading = false;
        }),
        catchError((err) => {
          this.error = err.error?.message || 'Failed to load truck types';
          return of([] as TruckType[]);
        })
      )
      .subscribe((rows) => {
        this.truckTypes = rows;
      });
  }

  loadTrucks(): void {
    this.loading = true;
    this.error = null;
    this.truckService
      .getAll(true)
      .pipe(
        finalize(() => (this.loading = false)),
        catchError((err) => {
          this.error = err.error?.message || 'Failed to load trucks';
          return of([] as TruckListItem[]);
        })
      )
      .subscribe((rows) => {
        this.trucks = rows;
      });
  }

  openCreateType(): void {
    this.editingType = null;
    this.typeForm.reset({ name: '', description: '' });
    this.showTypeModal = true;
  }

  openEditType(t: TruckType): void {
    this.editingType = t;
    this.typeForm.patchValue({
      name: t.name,
      description: t.description || ''
    });
    this.showTypeModal = true;
  }

  closeTypeModal(): void {
    this.showTypeModal = false;
    this.editingType = null;
  }

  saveType(): void {
    if (this.typeForm.invalid) {
      this.typeForm.markAllAsTouched();
      return;
    }
    this.typeSubmitting = true;
    const v = this.typeForm.value as CreateTruckTypeRequest;
    const req$ = this.editingType
      ? this.truckTypeService.update(this.editingType.id, {
          name: v.name.trim(),
          description: v.description?.trim() || undefined
        })
      : this.truckTypeService.create({
          name: v.name.trim(),
          description: v.description?.trim() || undefined
        });

    req$.pipe(finalize(() => (this.typeSubmitting = false))).subscribe({
      next: () => {
        this.closeTypeModal();
        this.loadTruckTypes();
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to save truck type';
      }
    });
  }

  deleteType(t: TruckType): void {
    if (!confirm(`Delete truck type "${t.name}"?`)) {
      return;
    }
    this.truckTypeService.delete(t.id).subscribe({
      next: () => this.loadTruckTypes(),
      error: (err) => {
        this.error = err.error?.message || 'Cannot delete truck type';
      }
    });
  }

  openCreateTruck(): void {
    this.editingTruck = null;
    this.truckForm.reset({
      truckTypeId: this.truckTypes[0]?.id ?? null,
      unitLabel: '',
      licensePlate: '',
      vin: '',
      make: '',
      model: '',
      year: null,
      notes: '',
      isOutOfService: false,
      isActive: true
    });
    this.showTruckModal = true;
  }

  openEditTruck(t: TruckListItem): void {
    this.editingTruck = t;
    this.truckForm.patchValue({
      truckTypeId: t.truckTypeId,
      unitLabel: t.unitLabel,
      licensePlate: t.licensePlate || '',
      vin: t.vin || '',
      make: t.make || '',
      model: t.model || '',
      year: t.year ?? null,
      notes: t.notes || '',
      isOutOfService: t.isOutOfService,
      isActive: t.isActive
    });
    this.showTruckModal = true;
  }

  closeTruckModal(): void {
    this.showTruckModal = false;
    this.editingTruck = null;
  }

  saveTruck(): void {
    if (this.truckForm.invalid) {
      this.truckForm.markAllAsTouched();
      return;
    }
    const raw = this.truckForm.value;
    this.truckSubmitting = true;

    const base: CreateTruckRequest = {
      truckTypeId: Number(raw.truckTypeId),
      unitLabel: (raw.unitLabel as string).trim(),
      licensePlate: (raw.licensePlate as string)?.trim() || undefined,
      vin: (raw.vin as string)?.trim() || undefined,
      make: (raw.make as string)?.trim() || undefined,
      model: (raw.model as string)?.trim() || undefined,
      year: raw.year != null && raw.year !== '' ? Number(raw.year) : undefined,
      notes: (raw.notes as string)?.trim() || undefined,
      isOutOfService: !!raw.isOutOfService
    };

    const req$ = this.editingTruck
      ? this.truckService.update(this.editingTruck.id, {
          ...base,
          isActive: !!raw.isActive
        } as UpdateTruckRequest)
      : this.truckService.create(base);

    req$.pipe(finalize(() => (this.truckSubmitting = false))).subscribe({
      next: () => {
        this.closeTruckModal();
        this.loadTrucks();
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to save truck';
      }
    });
  }

  softDeleteTruck(t: TruckListItem): void {
    if (!confirm(`Remove truck "${t.unitLabel}" from the fleet?`)) {
      return;
    }
    this.truckService.delete(t.id).subscribe({
      next: () => this.loadTrucks(),
      error: (err) => {
        this.error = err.error?.message || 'Failed to delete truck';
      }
    });
  }

  availabilityBadgeClass(label: string): string {
    if (label === 'Out of service') {
      return 'bg-amber-100 text-amber-900';
    }
    if (label === 'Busy') {
      return 'bg-orange-100 text-orange-900';
    }
    return 'bg-green-100 text-green-900';
  }
}
