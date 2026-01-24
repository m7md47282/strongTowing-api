import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { catchError, finalize } from 'rxjs/operators';
import { of } from 'rxjs';
import { VehicleService, Vehicle, CreateVehicleRequest } from '../../../../services/vehicle.service';

@Component({
  selector: 'app-vehicles',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './vehicles.component.html',
  styleUrls: ['./vehicles.component.scss']
})
export class VehiclesComponent implements OnInit {
  vehicles: Vehicle[] = [];
  filteredVehicles: Vehicle[] = [];
  loading = false;
  error: string | null = null;
  
  showCreateModal = false;
  createVehicleForm: FormGroup;
  submitting = false;
  
  searchTerm: string = '';
  maxYear: number;

  constructor(
    private vehicleService: VehicleService,
    private fb: FormBuilder
  ) {
    this.maxYear = new Date().getFullYear() + 1;
    this.createVehicleForm = this.fb.group({
      vin: ['', [Validators.required, Validators.minLength(17), Validators.maxLength(17)]],
      make: ['', [Validators.required]],
      model: ['', [Validators.required]],
      year: ['', [Validators.required, Validators.min(1900), Validators.max(this.maxYear)]],
      color: ['']
    });
  }

  ngOnInit(): void {
    this.loadVehicles();
  }

  loadVehicles(): void {
    this.loading = true;
    this.error = null;
    
    this.vehicleService.getAllVehicles()
      .pipe(
        finalize(() => this.loading = false),
        catchError(error => {
          this.error = error.error?.message || 'Failed to load vehicles';
          return of([]);
        })
      )
      .subscribe(vehicles => {
        this.vehicles = vehicles;
        this.applyFilters();
      });
  }

  applyFilters(): void {
    this.filteredVehicles = this.vehicles.filter(vehicle => {
      if (!this.searchTerm) return true;
      const search = this.searchTerm.toLowerCase();
      return vehicle.vin.toLowerCase().includes(search) ||
             vehicle.make.toLowerCase().includes(search) ||
             vehicle.model.toLowerCase().includes(search) ||
             vehicle.year.toString().includes(search) ||
             vehicle.color?.toLowerCase().includes(search);
    });
  }

  onSearchChange(): void {
    this.applyFilters();
  }

  openCreateModal(): void {
    this.showCreateModal = true;
    this.createVehicleForm.reset();
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
    this.createVehicleForm.reset();
    this.error = null;
  }

  onCreateVehicle(): void {
    if (this.createVehicleForm.invalid) {
      this.createVehicleForm.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.error = null;

    const vehicleData: CreateVehicleRequest = {
      vin: this.createVehicleForm.value.vin.toUpperCase(),
      make: this.createVehicleForm.value.make,
      model: this.createVehicleForm.value.model,
      year: parseInt(this.createVehicleForm.value.year),
      color: this.createVehicleForm.value.color || undefined
    };

    this.vehicleService.createVehicle(vehicleData)
      .pipe(
        finalize(() => this.submitting = false),
        catchError(error => {
          this.error = error.error?.message || 'Failed to create vehicle';
          return of(null);
        })
      )
      .subscribe(vehicle => {
        if (vehicle) {
          this.closeCreateModal();
          this.loadVehicles();
        }
      });
  }

  searchByVIN(): void {
    const vin = this.searchTerm.trim().toUpperCase();
    if (vin.length === 17) {
      this.loading = true;
      this.vehicleService.getVehicleByVIN(vin)
        .pipe(
          finalize(() => this.loading = false),
          catchError(error => {
            this.error = error.error?.message || 'Vehicle not found';
            return of(null);
          })
        )
        .subscribe(vehicle => {
          if (vehicle) {
            this.filteredVehicles = [vehicle];
          }
        });
    }
  }
}

