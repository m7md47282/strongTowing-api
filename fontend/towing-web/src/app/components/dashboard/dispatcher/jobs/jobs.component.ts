import { ChangeDetectorRef, Component, NgZone, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators, FormControl, AbstractControl } from '@angular/forms';
import { HttpParams } from '@angular/common/http';
import {
  catchError,
  debounceTime,
  distinctUntilChanged,
  finalize,
  startWith,
  switchMap
} from 'rxjs/operators';
import { combineLatest, EMPTY, firstValueFrom, from, of, Subscription } from 'rxjs';
import { VehicleCatalogService, VehicleModelsPage } from '../../../../services/vehicle-catalog.service';
import { Loader } from '@googlemaps/js-api-loader';
import { JobService, Job, CreateJobRequest, AssignDriverRequest, AssignDriverResponse, VehicleData, ClientData, UpdateJobStatusRequest } from '../../../../services/job.service';
import { VehicleService, Vehicle } from '../../../../services/vehicle.service';
import { ApiService } from '../../../../services/api.service';
import { AccountsService } from '../../../../services/accounts.service';
import { ServicePricingService } from '../../../../services/service-pricing.service';
import { AuthService } from '../../../../services/auth.service';
import { PaymentService } from '../../../../services/payment.service';
import { InsuranceAccount } from '../../../../models/insurance-account.model';
import { ServicePricingProfile } from '../../../../models/service-pricing.model';
import { User } from '../../../../models/user.model';
import { RoleId } from '../../../../constants/user-roles.constants';
import { PlacesAutocompleteDirective } from '../../../../directives/places-autocomplete.directive';
import { LocationPickerComponent } from '../../../shared/location-picker/location-picker.component';
import { LocationService } from '../../../../services/location.service';
import { PricingService, PricingQuoteResponse } from '../../../../services/pricing.service';
import { environment } from '../../../../../environments/environment';

interface PagedResponse<T> {
  data: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

@Component({
  selector: 'app-jobs',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    PlacesAutocompleteDirective,
    LocationPickerComponent
  ],
  templateUrl: './jobs.component.html',
  styleUrls: ['./jobs.component.scss']
})
export class JobsComponent implements OnInit, OnDestroy {
  jobs: Job[] = [];
  filteredJobs: Job[] = [];
  loading = false;
  error: string | null = null;
  /** Shown after assign succeeds but FCM push was not delivered (no token, Firebase off, etc.) */
  pushNotificationWarning: string | null = null;
  createJobError: string | null = null;
  
  // Modals
  showCreateJobModal = false;
  showAssignDriverModal = false;
  showJobDetailsModal = false;
  showUpdateStatusModal = false;
  showPaymentLinkModal = false;
  selectedJob: Job | null = null;

  /** Stripe payment link for a job (dispatcher / admin / super admin) */
  paymentLinkJob: Job | null = null;
  paymentLinkUrl: string | null = null;
  paymentLinkError: string | null = null;
  paymentLinkSubmitting = false;
  paymentLinkCopied = false;
  quickPaySubmittingJobId: number | null = null;
  
  // Forms
  createJobForm: FormGroup;
  assignDriverForm: FormGroup;
  updateStatusControl = new FormControl<string>('', { nonNullable: true, validators: [Validators.required] });
  submitting = false;
  
  // Filters
  statusFilter: string = '';
  searchTerm: string = '';
  
  // Data for dropdowns
  vehicles: Vehicle[] = [];
  filteredVehicles: Vehicle[] = [];
  clients: User[] = [];
  drivers: User[] = [];
  availableDrivers: User[] = [];
  
  // Selection modes
  useExistingClient: boolean = true;
  useExistingVehicle: boolean = true;
  
  // Validation error list
  validationErrorList: string[] = [];

  /** After user clicks Create Job once, show inline errors even if fields were not touched */
  createJobValidationAttempted = false;

  // Create job modal tabs
  activeCreateJobTab: 'details' | 'payment' = 'details';
  
  // Status options
  statusOptions = [
    { value: '', label: 'All Statuses' },
    { value: 'Pending', label: 'Pending' },
    { value: 'Assigned', label: 'Assigned' },
    { value: 'OnRoute', label: 'On Route' },
    { value: 'InProgress', label: 'In Progress' },
    { value: 'ReadyToRelease', label: 'Ready to Release' },
    { value: 'Completed', label: 'Completed' }
  ];
  
  statusProgression: Job['status'][] = [
    'Pending',
    'Assigned',
    'OnRoute',
    'InProgress',
    'ReadyToRelease',
    'Completed'
  ];
  
  // Service types are loaded from backend service pricing profiles.
  serviceTypes: string[] = [];

  // Call types
  callTypes = ['New Call', 'Completed Call', 'Schedule a call', 'Quote'];
  
  // Priority options
  priorities = ['Normal', 'Emergency', 'Low'];
  
  // Vehicle types
  vehicleTypes = ['Light', 'Medium', 'Heavy', 'Motorcycle', 'RV', 'Other'];
  
  // Drive types
  driveTypes = ['FWD', 'RWD', 'AWD', '4WD', 'Unknown'];
  
  // Drivable options
  drivableOptions = ['Yes', 'No'];
  
  // Trucks (placeholder - you may need to load from a service)
  trucks: any[] = [];
  
  // Service items for invoice charges
  serviceItems: any[] = [];
  
  // Invoice charges form arrays
  invoiceServiceItems: any[] = [];

  /** Insurance accounts for job Account dropdown (active only) */
  insuranceAccounts: InsuranceAccount[] = [];
  insuranceAccountsLoading = false;
  servicePricingProfiles: ServicePricingProfile[] = [];
  servicePricingProfilesLoading = false;

  /** Map picker sub-modal for pickup / destination (above create-job modal) */
  showLocationMapModal = false;
  locationMapTarget: 'pickup' | 'destination' | null = null;
  mapPickerLat: number | null = null;
  mapPickerLng: number | null = null;

  /** Driving distance & time pickup → destination, when both addresses are set */
  pickupToDestinationMiles: number | null = null;
  pickupToDestinationDurationText: string | null = null;
  pickupToDestinationMilesLoading = false;
  pickupToDestinationMilesError: string | null = null;
  officeToPickupMiles: number | null = null;
  dropoffToOfficeMiles: number | null = null;
  /** Dispatch office (point A) from Admin → Settings; used for pricing legs and map centering. */
  dispatchOfficeCoords: { lat: number; lng: number } | null = null;
  dispatchOfficeLoadError: string | null = null;
  calculatingPrice = false;
  latestQuote: PricingQuoteResponse | null = null;
  private routeDistanceGeneration = 0;
  private pickupDestinationDistanceSub?: Subscription;
  private vehicleMakeSub?: Subscription;

  /** NHTSA vPIC (US) — loaded when creating a new vehicle */
  vehicleMakes: string[] = [];
  vehicleModels: string[] = [];
  vehicleMakesLoading = false;
  vehicleModelsLoading = false;
  vehicleModelsLoadingMore = false;
  vehicleMakesError: string | null = null;
  /** Server-side cache makes subsequent pages fast; we track total for UI. */
  vehicleModelsTotalCount = 0;
  vehicleModelsHasMore = false;
  private vehicleModelsLastLoadedPage = 0;
  private readonly vehicleModelPageSize = 100;
  private readonly makesSessionStorageKey = 'st_vehicle_makes_v1';

  constructor(
    private jobService: JobService,
    private vehicleService: VehicleService,
    private apiService: ApiService,
    private accountsService: AccountsService,
    private servicePricingService: ServicePricingService,
    private authService: AuthService,
    private paymentService: PaymentService,
    private locationService: LocationService,
    private pricingService: PricingService,
    private vehicleCatalogService: VehicleCatalogService,
    private ngZone: NgZone,
    private cdr: ChangeDetectorRef,
    private fb: FormBuilder
  ) {
    // Client section
    const clientGroup = this.fb.group({
      clientId: [''],
      // New client fields
      clientEmail: [''], // Optional - no validators
      clientFullName: ['', [Validators.minLength(2)]],
      clientPhoneNumber: [''] // Required - main key (will be set as required when creating new client)
    });
    
    // Vehicle section
    const vehicleGroup = this.fb.group({
      vehicleId: [''],
      // New vehicle fields
      vehicleVin: [''],
      vehicleMake: [''],
      vehicleModel: [''],
      vehicleYear: ['', [Validators.min(1900), Validators.max(2100)]],
      vehicleColor: [''],
      licensePlate: [''],
      licenseState: [''],
      driveType: [''],
      vehicleType: [''],
      odometer: [''],
      drivable: [''],
      haveKeys: [false],
      keyLocation: ['']
    });
    
    // Invoice charges section
    const invoiceChargesGroup = this.fb.group({
      unloadedEnrouteMileageQuantity: [0],
      unloadedEnrouteMileagePrice: [0],
      loadedHookedMileageQuantity: [0],
      loadedHookedMileagePrice: [0],
      deadHeadMileageQuantity: [0],
      deadHeadMileagePrice: [0],
      hookupFee: [0],
      discount: [0],
      discountPercent: [0],
      serviceChargePercent: [0],
      taxPercent: [10],
      taxExempt: [false],
      manualTotalOverride: [null as number | null],
      manualOverrideReason: ['']
    });
    
    this.createJobForm = this.fb.group({
      // Client group
      client: clientGroup,
      // Vehicle group
      vehicle: vehicleGroup,
      // Call/Job Type
      callType: ['New Call'],
      scheduledDate: [''],
      scheduledTime: [''],
      // Company & Account
      companyName: ['Strong Towing Inc'],
      account: [''],
      companyOverride: [''],
      // Contact Information
      contactName: [''],
      contactPhoneNumber: [''],
      // Location
      pickupLocation: ['', [Validators.required]],
      destinationAddress: [''],
      // Job Details
      priority: ['Normal'],
      eta: [''],
      // Assignment
      driverId: [''],
      truckId: [''],
      // Notes
      notes: [''],
      billingNotes: [''],
      includeBillingNotesOnReceipt: [false],
      // Invoice Charges
      invoiceCharges: invoiceChargesGroup,
      // Job fields
      serviceType: ['', [Validators.required]],
      dropoffLocation: [''], // Alias for destinationAddress
      cost: ['', [Validators.required, Validators.min(0.01), Validators.max(999999.99)]]
    });
    
    this.assignDriverForm = this.fb.group({
      driverId: ['', [Validators.required]]
    });
    
    // Add custom validators for either/or logic
    this.setupFormValidators();
  }
  
  setupFormValidators(): void {
    // Client validation: either clientId OR client data
    this.createJobForm.get('client')?.valueChanges.subscribe(() => {
      this.validateClientGroup();
      // Clear error when user starts fixing fields
      if (this.createJobError && this.validationErrorList.length > 0) {
        this.createJobError = null;
        this.validationErrorList = [];
      }
    });
    
    // Vehicle validation: either vehicleId OR vehicle data
    this.createJobForm.get('vehicle')?.valueChanges.subscribe(() => {
      this.validateVehicleGroup();
      // Clear error when user starts fixing fields
      if (this.createJobError && this.validationErrorList.length > 0) {
        this.createJobError = null;
        this.validationErrorList = [];
      }
    });
    
    // Subscribe to individual field changes
    this.createJobForm.get('serviceType')?.valueChanges.subscribe(() => {
      this.applySelectedServicePricing();
      if (this.createJobError && this.validationErrorList.length > 0) {
        this.createJobError = null;
        this.validationErrorList = [];
      }
    });
    
    this.createJobForm.get('pickupLocation')?.valueChanges.subscribe(() => {
      if (this.createJobError && this.validationErrorList.length > 0) {
        this.createJobError = null;
        this.validationErrorList = [];
      }
    });
    
    this.createJobForm.get('cost')?.valueChanges.subscribe(() => {
      if (this.createJobError && this.validationErrorList.length > 0) {
        this.createJobError = null;
        this.validationErrorList = [];
      }
    });
  }
  
  validateClientGroup(): void {
    const clientGroup = this.createJobForm.get('client');
    if (!clientGroup) return;
    
    if (this.useExistingClient) {
      // Require clientId
      clientGroup.get('clientId')?.setValidators([Validators.required]);
      clientGroup.get('clientEmail')?.clearValidators();
      clientGroup.get('clientFullName')?.clearValidators();
      clientGroup.get('clientPhoneNumber')?.clearValidators();
    } else {
      // Require new client fields - phone number is the main key
      clientGroup.get('clientId')?.clearValidators();
      clientGroup.get('clientEmail')?.setValidators([Validators.email]); // Optional, but must be valid if provided
      clientGroup.get('clientFullName')?.setValidators([Validators.required, Validators.minLength(2)]);
      clientGroup.get('clientPhoneNumber')?.setValidators([Validators.required]); // Required - main key
    }
    
    clientGroup.get('clientId')?.updateValueAndValidity({ emitEvent: false });
    clientGroup.get('clientEmail')?.updateValueAndValidity({ emitEvent: false });
    clientGroup.get('clientFullName')?.updateValueAndValidity({ emitEvent: false });
    clientGroup.get('clientPhoneNumber')?.updateValueAndValidity({ emitEvent: false });
    
    // Update parent form group validity
    clientGroup.updateValueAndValidity({ emitEvent: false });
    this.createJobForm.updateValueAndValidity({ emitEvent: false });
  }
  
  validateVehicleGroup(): void {
    const vehicleGroup = this.createJobForm.get('vehicle');
    if (!vehicleGroup) return;
    
    if (this.useExistingVehicle) {
      // Require vehicleId
      vehicleGroup.get('vehicleId')?.setValidators([Validators.required]);
      vehicleGroup.get('vehicleVin')?.clearValidators();
      vehicleGroup.get('vehicleMake')?.clearValidators();
      vehicleGroup.get('vehicleModel')?.clearValidators();
      vehicleGroup.get('vehicleYear')?.clearValidators();
      vehicleGroup.get('vehicleColor')?.clearValidators();
    } else {
      // Require new vehicle fields (VIN optional)
      vehicleGroup.get('vehicleId')?.clearValidators();
      vehicleGroup.get('vehicleVin')?.clearValidators();
      vehicleGroup.get('vehicleMake')?.setValidators([Validators.required]);
      vehicleGroup.get('vehicleModel')?.setValidators([Validators.required]);
      vehicleGroup.get('vehicleYear')?.setValidators([Validators.required, Validators.min(1900), Validators.max(2100)]);
      vehicleGroup.get('vehicleColor')?.clearValidators();
    }
    
    vehicleGroup.get('vehicleId')?.updateValueAndValidity({ emitEvent: false });
    vehicleGroup.get('vehicleVin')?.updateValueAndValidity({ emitEvent: false });
    vehicleGroup.get('vehicleMake')?.updateValueAndValidity({ emitEvent: false });
    vehicleGroup.get('vehicleModel')?.updateValueAndValidity({ emitEvent: false });
    vehicleGroup.get('vehicleYear')?.updateValueAndValidity({ emitEvent: false });
    vehicleGroup.get('vehicleColor')?.updateValueAndValidity({ emitEvent: false });
    
    // Update parent form group validity
    vehicleGroup.updateValueAndValidity({ emitEvent: false });
    this.createJobForm.updateValueAndValidity({ emitEvent: false });
  }

  /** Show red border / message when the control is invalid and (touched or user tried to submit). */
  showCreateJobFieldError(control: AbstractControl | null): boolean {
    if (!control) {
      return false;
    }
    return (control.touched || this.createJobValidationAttempted) && control.invalid;
  }

  ngOnInit(): void {
    this.loadJobs();
    this.loadVehicles();
    this.loadClients();
    this.loadDrivers();
    this.loadServicePricingProfiles();
    this.loadDispatchOfficeForDisplay();
    this.observeSelectedClientChanges();

    this.vehicleMakeSub = this.createJobForm.get('vehicle.vehicleMake')?.valueChanges.subscribe((make) => {
      if (this.useExistingVehicle) {
        return;
      }
      this.createJobForm.get('vehicle.vehicleModel')?.patchValue('', { emitEvent: false });
      const trimmed = (make ?? '').toString().trim();
      if (!trimmed) {
        this.resetVehicleModelsPagination();
        return;
      }
      this.loadModelsForMake(trimmed, false);
    });

    const pickupCtrl = this.createJobForm.get('pickupLocation')!;
    const destCtrl = this.createJobForm.get('destinationAddress')!;
    this.pickupDestinationDistanceSub = combineLatest([
      pickupCtrl.valueChanges.pipe(startWith(pickupCtrl.value)),
      destCtrl.valueChanges.pipe(startWith(destCtrl.value))
    ])
      .pipe(
        debounceTime(500),
        distinctUntilChanged(
          (a, b) =>
            (a[0] ?? '').trim() === (b[0] ?? '').trim() &&
            (a[1] ?? '').trim() === (b[1] ?? '').trim()
        ),
        switchMap(([p, d]) => {
          const pu = (p ?? '').trim();
          const de = (d ?? '').trim();
          if (!pu || !de) {
            this.ngZone.run(() => {
              this.pickupToDestinationMiles = null;
              this.pickupToDestinationDurationText = null;
              this.pickupToDestinationMilesError = null;
              this.pickupToDestinationMilesLoading = false;
              this.routeDistanceGeneration += 1;
              this.cdr.markForCheck();
            });
            return EMPTY;
          }
          return from(this.loadPickupToDestinationMiles(pu, de));
        })
      )
      .subscribe();
  }

  ngOnDestroy(): void {
    this.pickupDestinationDistanceSub?.unsubscribe();
    this.vehicleMakeSub?.unsubscribe();
  }

  /** Retry loading NHTSA makes after a failure (template). */
  retryVehicleMakes(): void {
    try {
      sessionStorage.removeItem(this.makesSessionStorageKey);
    } catch {
      /* ignore */
    }
    this.vehicleMakes = [];
    this.vehicleMakesError = null;
    this.loadVehicleMakes();
  }

  /** Load next page of models after the first page (server caches full list per make). */
  loadMoreVehicleModels(): void {
    const make = (this.createJobForm.get('vehicle.vehicleMake')?.value ?? '').toString().trim();
    if (!make || !this.vehicleModelsHasMore || this.vehicleModelsLoadingMore) {
      return;
    }
    this.loadModelsForMake(make, true);
  }

  private loadVehicleMakes(): void {
    if (this.vehicleMakes.length > 0 || this.vehicleMakesLoading) {
      return;
    }
    try {
      const raw = sessionStorage.getItem(this.makesSessionStorageKey);
      if (raw) {
        const parsed = JSON.parse(raw) as unknown;
        if (Array.isArray(parsed) && parsed.length > 0 && parsed.every((x) => typeof x === 'string')) {
          this.vehicleMakes = parsed as string[];
          return;
        }
      }
    } catch {
      /* ignore */
    }
    this.vehicleMakesLoading = true;
    this.vehicleMakesError = null;
    this.vehicleCatalogService
      .getMakes()
      .pipe(
        catchError(() => {
          this.vehicleMakesError = 'Could not load vehicle makes.';
          return of([] as string[]);
        }),
        finalize(() => {
          this.vehicleMakesLoading = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe((makes) => {
        this.vehicleMakes = makes;
        try {
          if (makes.length > 0) {
            sessionStorage.setItem(this.makesSessionStorageKey, JSON.stringify(makes));
          }
        } catch {
          /* ignore */
        }
      });
  }

  private emptyModelsPage(): VehicleModelsPage {
    return {
      models: [],
      totalCount: 0,
      page: 1,
      pageSize: this.vehicleModelPageSize,
      totalPages: 0
    };
  }

  private resetVehicleModelsPagination(): void {
    this.vehicleModels = [];
    this.vehicleModelsTotalCount = 0;
    this.vehicleModelsHasMore = false;
    this.vehicleModelsLastLoadedPage = 0;
  }

  private loadModelsForMake(make: string, append: boolean): void {
    if (append) {
      if (!this.vehicleModelsHasMore || this.vehicleModelsLoadingMore) {
        return;
      }
      this.vehicleModelsLoadingMore = true;
    } else {
      this.vehicleModelsLoading = true;
      this.vehicleModels = [];
      this.vehicleModelsTotalCount = 0;
      this.vehicleModelsHasMore = false;
      this.vehicleModelsLastLoadedPage = 0;
    }

    const page = append ? this.vehicleModelsLastLoadedPage + 1 : 1;

    this.vehicleCatalogService
      .getModelsPage(make, page, this.vehicleModelPageSize)
      .pipe(
        catchError(() => of(this.emptyModelsPage())),
        finalize(() => {
          if (append) {
            this.vehicleModelsLoadingMore = false;
          } else {
            this.vehicleModelsLoading = false;
          }
          this.cdr.markForCheck();
        })
      )
      .subscribe((res) => {
        this.vehicleModelsTotalCount = res.totalCount;
        this.vehicleModelsLastLoadedPage = res.page;
        this.vehicleModelsHasMore = res.page < res.totalPages;
        if (append) {
          this.vehicleModels = [...this.vehicleModels, ...res.models];
        } else {
          this.vehicleModels = res.models;
        }
      });
  }

  private loadDispatchOfficeForDisplay(): void {
    this.locationService.getOfficeLocation().subscribe({
      next: (o) => {
        this.dispatchOfficeCoords = { lat: o.lat, lng: o.lng };
        this.dispatchOfficeLoadError = null;
        this.cdr.markForCheck();
      },
      error: () => {
        this.dispatchOfficeCoords = null;
        this.dispatchOfficeLoadError = 'Could not load dispatch office location.';
        this.cdr.markForCheck();
      }
    });
  }

  private async loadPickupToDestinationMiles(pickup: string, dest: string): Promise<void> {
    const gen = ++this.routeDistanceGeneration;
    this.ngZone.run(() => {
      this.pickupToDestinationMilesLoading = true;
      this.pickupToDestinationMilesError = null;
      this.cdr.markForCheck();
    });

    const apiKey = environment.mapsApiKey?.trim();
    if (!apiKey) {
      if (gen !== this.routeDistanceGeneration) {
        return;
      }
      this.ngZone.run(() => {
        this.pickupToDestinationMiles = null;
        this.pickupToDestinationDurationText = null;
        this.pickupToDestinationMilesError = 'Maps API key is not configured.';
        this.pickupToDestinationMilesLoading = false;
        this.cdr.markForCheck();
      });
      return;
    }

    try {
      const loader = new Loader({ apiKey, version: 'weekly', libraries: ['places'] });
      await loader.load();
    } catch {
      if (gen !== this.routeDistanceGeneration) {
        return;
      }
      this.ngZone.run(() => {
        this.pickupToDestinationMiles = null;
        this.pickupToDestinationDurationText = null;
        this.pickupToDestinationMilesError = 'Could not load Google Maps.';
        this.pickupToDestinationMilesLoading = false;
        this.cdr.markForCheck();
      });
      return;
    }

    if (gen !== this.routeDistanceGeneration) {
      return;
    }

    const ds = new google.maps.DirectionsService();
    await new Promise<void>((resolve) => {
      ds.route(
        {
          origin: pickup,
          destination: dest,
          travelMode: google.maps.TravelMode.DRIVING,
          unitSystem: google.maps.UnitSystem.IMPERIAL
        },
        (result, status) => {
          this.ngZone.run(() => {
            if (gen !== this.routeDistanceGeneration) {
              resolve();
              return;
            }
            this.pickupToDestinationMilesLoading = false;
            if (status === google.maps.DirectionsStatus.OK && result?.routes?.[0]) {
              let meters = 0;
              let seconds = 0;
              for (const leg of result.routes[0].legs) {
                meters += leg.distance?.value ?? 0;
                seconds += leg.duration?.value ?? 0;
              }
              const miles = meters * 0.000621371;
              this.pickupToDestinationMiles = Math.round(miles * 100) / 100;
              this.pickupToDestinationDurationText = this.formatDrivingDurationSeconds(seconds);
              this.pickupToDestinationMilesError = null;
            } else {
              this.pickupToDestinationMiles = null;
              this.pickupToDestinationDurationText = null;
              this.pickupToDestinationMilesError = 'Could not compute driving distance.';
            }
            this.cdr.markForCheck();
            resolve();
          });
        }
      );
    });
  }

  /** Sum of Directions leg durations → human-readable drive time */
  private formatDrivingDurationSeconds(totalSeconds: number): string {
    const totalMins = Math.max(1, Math.ceil(totalSeconds / 60));
    if (totalMins < 60) {
      return `${totalMins} min`;
    }
    const h = Math.floor(totalMins / 60);
    const m = totalMins % 60;
    if (m === 0) {
      return `${h} h`;
    }
    return `${h} h ${m} min`;
  }

  loadJobs(): void {
    this.loading = true;
    this.error = null;
    
    this.jobService.getAllJobs(this.statusFilter || undefined)
      .pipe(
        finalize(() => this.loading = false),
        catchError(error => {
          this.error = error.error?.message || 'Failed to load jobs';
          return of([]);
        })
      )
      .subscribe(jobs => {
        this.jobs = jobs;
        this.applyFilters();
      });
  }

  loadVehicles(): void {
    this.vehicleService.getAllVehicles()
      .pipe(
        catchError(error => {
          console.error('Failed to load vehicles:', error);
          return of([]);
        })
      )
      .subscribe(vehicles => {
        this.vehicles = vehicles;
        this.syncVehicleOptionsForCurrentClient();
      });
  }

  private observeSelectedClientChanges(): void {
    const clientIdControl = this.createJobForm.get('client.clientId');
    clientIdControl?.valueChanges
      .pipe(
        startWith(clientIdControl.value),
        distinctUntilChanged()
      )
      .subscribe(() => {
        this.syncVehicleOptionsForCurrentClient();
      });
  }

  private syncVehicleOptionsForCurrentClient(): void {
    if (!this.useExistingClient) {
      this.filteredVehicles = this.vehicles;
      return;
    }

    const selectedClientId = this.getSelectedClientId();
    if (!selectedClientId) {
      this.filteredVehicles = [];
      this.clearExistingVehicleSelection();
      return;
    }

    this.vehicleService.getAllVehicles(selectedClientId)
      .pipe(
        catchError(error => {
          console.error('Failed to load client vehicles:', error);
          return of([]);
        })
      )
      .subscribe(vehicles => {
        this.filteredVehicles = vehicles;
        this.clearSelectedVehicleIfNotInList();
      });
  }

  private getSelectedClientId(): string {
    return this.createJobForm.get('client.clientId')?.value?.toString().trim() || '';
  }

  private clearExistingVehicleSelection(): void {
    if (!this.useExistingVehicle) {
      return;
    }

    this.createJobForm.get('vehicle.vehicleId')?.patchValue('', { emitEvent: false });
  }

  private clearSelectedVehicleIfNotInList(): void {
    if (!this.useExistingVehicle) {
      return;
    }

    const selectedVehicleIdValue = this.createJobForm.get('vehicle.vehicleId')?.value;
    if (!selectedVehicleIdValue) {
      return;
    }

    const selectedVehicleId = Number(selectedVehicleIdValue);
    const exists = this.filteredVehicles.some(vehicle => vehicle.id === selectedVehicleId);
    if (!exists) {
      this.createJobForm.get('vehicle.vehicleId')?.patchValue('', { emitEvent: false });
    }
  }

  loadClients(): void {
    // Use dedicated clients endpoint with pagination
    // Load all active clients (using large page size for dropdown)
    const params = new HttpParams()
      .set('pageNumber', '1')
      .set('pageSize', '100')
      .set('isActive', 'true');
    
    this.apiService.get<PagedResponse<User>>('users/clients', params)
      .pipe(
        catchError(error => {
          console.error('Failed to load clients:', error);
          return of({ data: [], pageNumber: 1, pageSize: 100, totalCount: 0, totalPages: 0, hasPreviousPage: false, hasNextPage: false });
        })
      )
      .subscribe(response => {
        this.clients = response.data;
      });
  }

  loadDrivers(): void {
    // Use dedicated drivers endpoint with pagination
    // Load all active drivers (using large page size for dropdown)
    const params = new HttpParams()
      .set('pageNumber', '1')
      .set('pageSize', '100')
      .set('isActive', 'true')
      .set('availableForDispatchOnly', 'true');
    
    this.apiService.get<PagedResponse<User>>('users/drivers', params)
      .pipe(
        catchError(error => {
          console.error('Failed to load drivers:', error);
          return of({ data: [], pageNumber: 1, pageSize: 100, totalCount: 0, totalPages: 0, hasPreviousPage: false, hasNextPage: false });
        })
      )
      .subscribe(response => {
        this.drivers = response.data;
        this.availableDrivers = response.data.filter(d => d.isActive);
      });
  }

  applyFilters(): void {
    this.filteredJobs = this.jobs.filter(job => {
      const matchesStatus = !this.statusFilter || job.status === this.statusFilter;
      const matchesSearch = !this.searchTerm || 
        job.id.toString().includes(this.searchTerm) ||
        job.vehicle?.make?.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        job.vehicle?.model?.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        job.driverName?.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        job.serviceType?.toLowerCase().includes(this.searchTerm.toLowerCase());
      
      return matchesStatus && matchesSearch;
    });
  }

  onStatusFilterChange(): void {
    this.loadJobs();
  }

  onSearchChange(): void {
    this.applyFilters();
  }

  openCreateJobModal(): void {
    this.showCreateJobModal = true;
    this.activeCreateJobTab = 'details';
    this.useExistingClient = true;
    this.useExistingVehicle = true;
    this.vehicleMakesError = null;
    this.invoiceServiceItems = [];
    this.createJobValidationAttempted = false;
    this.createJobForm.reset();
    this.createJobForm.patchValue({
      callType: 'New Call',
      priority: 'Normal',
      companyName: 'Strong Towing Inc',
      invoiceCharges: {
        taxPercent: 10
      }
    });
    // Disable companyName field - it should not be editable
    this.createJobForm.get('companyName')?.disable();
    this.validateClientGroup();
    this.validateVehicleGroup();
    this.createJobError = null;
    this.latestQuote = null;
    this.officeToPickupMiles = null;
    this.dropoffToOfficeMiles = null;
    this.calculatingPrice = false;
    this.loadInsuranceAccountsForJob();
    this.loadServicePricingProfiles();
    this.syncVehicleOptionsForCurrentClient();
  }

  loadInsuranceAccountsForJob(): void {
    this.insuranceAccountsLoading = true;
    this.accountsService
      .getAll(false)
      .pipe(
        catchError(() => of([] as InsuranceAccount[])),
        finalize(() => (this.insuranceAccountsLoading = false))
      )
      .subscribe((rows) => {
        this.insuranceAccounts = rows;
      });
  }

  loadServicePricingProfiles(): void {
    this.servicePricingProfilesLoading = true;
    this.servicePricingService
      .getAll(false)
      .pipe(
        catchError(() => of([] as ServicePricingProfile[])),
        finalize(() => (this.servicePricingProfilesLoading = false))
      )
      .subscribe((rows) => {
        this.servicePricingProfiles = rows;
        this.serviceTypes = rows
          .filter((p) => p.isAvailable)
          .map((p) => p.name)
          .sort((a, b) => a.localeCompare(b));

        const selectedServiceType = String(this.createJobForm.get('serviceType')?.value || '').trim();
        if (
          selectedServiceType &&
          !this.serviceTypes.some((name) => name.toLowerCase() === selectedServiceType.toLowerCase())
        ) {
          this.createJobForm.patchValue({ serviceType: '' });
        }

        this.applySelectedServicePricing();
      });
  }

  private applySelectedServicePricing(): void {
    const serviceTypeRaw = this.createJobForm.get('serviceType')?.value;
    const selectedServiceType = String(serviceTypeRaw || '').trim();
    const charges = this.createJobForm.get('invoiceCharges');
    if (!charges) {
      return;
    }

    if (!selectedServiceType) {
      this.calculateInvoiceTotals();
      return;
    }

    const profile = this.servicePricingProfiles.find(
      (p) => p.isAvailable && p.name.toLowerCase() === selectedServiceType.toLowerCase()
    );

    if (!profile) {
      this.calculateInvoiceTotals();
      return;
    }

    // Reflect selected service prices in invoice charges.
    charges.patchValue({
      loadedHookedMileagePrice: profile.loadedPrice,
      deadHeadMileagePrice: profile.deadHeadPrice
    });

    this.calculateInvoiceTotals();
  }

  getSelectedServicePricingProfile(): ServicePricingProfile | null {
    const serviceTypeRaw = this.createJobForm.get('serviceType')?.value;
    const selectedServiceType = String(serviceTypeRaw || '').trim();
    if (!selectedServiceType) {
      return null;
    }

    return this.servicePricingProfiles.find(
      (p) => p.isAvailable && p.name.toLowerCase() === selectedServiceType.toLowerCase()
    ) ?? null;
  }

  insuranceAccountOptionLabel(account: InsuranceAccount): string {
    if (account.accountNumber) {
      return `${account.name} (${account.accountNumber})`;
    }
    return account.name;
  }
  
  onClientModeChange(): void {
    const clientGroup = this.createJobForm.get('client');
    if (clientGroup) {
      if (this.useExistingClient) {
        clientGroup.patchValue({
          clientEmail: '',
          clientFullName: '',
          clientPhoneNumber: ''
        });
      } else {
        clientGroup.patchValue({ clientId: '' });
      }
    }
    this.clearExistingVehicleSelection();
    this.syncVehicleOptionsForCurrentClient();
    this.validateClientGroup();
  }
  
  onVehicleModeChange(): void {
    const vehicleGroup = this.createJobForm.get('vehicle');
    if (vehicleGroup) {
      if (this.useExistingVehicle) {
        vehicleGroup.patchValue({
          vehicleVin: '',
          vehicleMake: '',
          vehicleModel: '',
          vehicleYear: '',
          vehicleColor: ''
        });
        this.resetVehicleModelsPagination();
      } else {
        vehicleGroup.patchValue({ vehicleId: '' });
        this.resetVehicleModelsPagination();
        this.loadVehicleMakes();
      }
    }
    this.validateVehicleGroup();
  }

  openLocationMapPicker(target: 'pickup' | 'destination'): void {
    this.locationMapTarget = target;
    this.mapPickerLat = null;
    this.mapPickerLng = null;
    this.locationService.getOfficeLocation().subscribe({
      next: (o) => {
        this.mapPickerLat = o.lat;
        this.mapPickerLng = o.lng;
        this.showLocationMapModal = true;
        this.cdr.markForCheck();
      },
      error: () => {
        this.showLocationMapModal = true;
        this.cdr.markForCheck();
      }
    });
  }

  closeLocationMapModal(): void {
    this.showLocationMapModal = false;
    this.locationMapTarget = null;
    this.mapPickerLat = null;
    this.mapPickerLng = null;
  }

  onLocationMapPositionChange(pos: { lat: number; lng: number }): void {
    const target = this.locationMapTarget;
    if (typeof google === 'undefined' || !google.maps?.Geocoder) {
      this.ngZone.run(() => {
        this.closeLocationMapModal();
        this.cdr.markForCheck();
      });
      return;
    }
    const geocoder = new google.maps.Geocoder();
    geocoder.geocode({ location: pos }, (results, status) => {
      this.ngZone.run(() => {
        if (status === 'OK' && results?.[0]?.formatted_address) {
          const addr = results[0].formatted_address;
          if (target === 'pickup') {
            this.createJobForm.patchValue({ pickupLocation: addr });
          } else if (target === 'destination') {
            this.createJobForm.patchValue({ destinationAddress: addr });
          }
        }
        this.closeLocationMapModal();
        this.cdr.markForCheck();
      });
    });
  }

  closeCreateJobModal(): void {
    this.closeLocationMapModal();
    this.showCreateJobModal = false;
    this.activeCreateJobTab = 'details';
    this.createJobForm.reset();
    this.invoiceServiceItems = [];
    this.createJobValidationAttempted = false;
    this.createJobError = null;
    this.validationErrorList = [];
  }

  setCreateJobTab(tab: 'details' | 'payment'): void {
    this.activeCreateJobTab = tab;
    if (tab === 'payment') {
      this.calculateInvoiceTotals();
    }
  }

  getValidationErrors(): string[] {
    const errors: string[] = [];
    const clientGroup = this.createJobForm.get('client');
    const vehicleGroup = this.createJobForm.get('vehicle');
    
    // Validate client section
    if (this.useExistingClient) {
      if (!clientGroup?.get('clientId')?.value) {
        errors.push('Client: Please select an existing client');
      }
    } else {
      if (!clientGroup?.get('clientPhoneNumber')?.value) {
        errors.push('Client Phone Number: Required field is missing');
      } else if (clientGroup?.get('clientPhoneNumber')?.invalid) {
        errors.push('Client Phone Number: Please enter a valid phone number');
      }
      if (!clientGroup?.get('clientFullName')?.value) {
        errors.push('Client Full Name: Required field is missing');
      } else if (clientGroup?.get('clientFullName')?.invalid) {
        errors.push('Client Full Name: Must be at least 2 characters');
      }
      if (clientGroup?.get('clientEmail')?.value && clientGroup?.get('clientEmail')?.invalid) {
        errors.push('Client Email: Please enter a valid email address');
      }
    }
    
    // Validate vehicle section
    if (this.useExistingVehicle) {
      if (!vehicleGroup?.get('vehicleId')?.value) {
        errors.push('Vehicle: Please select an existing vehicle');
      }
    } else {
      if (!vehicleGroup?.get('vehicleMake')?.value) {
        errors.push('Vehicle Make: Required field is missing');
      }
      if (!vehicleGroup?.get('vehicleModel')?.value) {
        errors.push('Vehicle Model: Required field is missing');
      }
      if (!vehicleGroup?.get('vehicleYear')?.value) {
        errors.push('Vehicle Year: Required field is missing');
      } else if (vehicleGroup?.get('vehicleYear')?.invalid) {
        errors.push('Vehicle Year: Must be between 1900 and 2100');
      }
    }
    
    // Validate job fields
    if (!this.createJobForm.get('serviceType')?.value) {
      errors.push('Service Type: Required field is missing');
    }
    if (!this.createJobForm.get('pickupLocation')?.value) {
      errors.push('Pickup Location: Required field is missing');
    }
    if (!this.createJobForm.get('cost')?.value) {
      errors.push('Cost: Required field is missing');
    } else if (this.createJobForm.get('cost')?.invalid) {
      const costControl = this.createJobForm.get('cost');
      if (costControl?.hasError('min')) {
        errors.push('Cost: Must be at least $0.01');
      } else if (costControl?.hasError('max')) {
        errors.push('Cost: Must be less than $999,999.99');
      } else {
        errors.push('Cost: Please enter a valid amount');
      }
    }
    
    return errors;
  }

  markAllFieldsAsTouched(): void {
    this.createJobForm.markAllAsTouched();
    const clientGroup = this.createJobForm.get('client');
    const vehicleGroup = this.createJobForm.get('vehicle');
    
    if (clientGroup) {
      if (this.useExistingClient) {
        clientGroup.get('clientId')?.markAsTouched();
      } else {
        clientGroup.get('clientPhoneNumber')?.markAsTouched();
        clientGroup.get('clientFullName')?.markAsTouched();
        if (clientGroup.get('clientEmail')?.value) {
          clientGroup.get('clientEmail')?.markAsTouched();
        }
      }
    }
    
    if (vehicleGroup) {
      if (this.useExistingVehicle) {
        vehicleGroup.get('vehicleId')?.markAsTouched();
      } else {
        vehicleGroup.get('vehicleVin')?.markAsTouched();
        vehicleGroup.get('vehicleMake')?.markAsTouched();
        vehicleGroup.get('vehicleModel')?.markAsTouched();
        vehicleGroup.get('vehicleYear')?.markAsTouched();
      }
    }
  }

  calculateInvoiceTotals(): void {
    const charges = this.createJobForm.get('invoiceCharges');
    if (!charges) return;
    
    const grandTotal = this.getGrandTotal();
    
    // Update cost field with grand total if it's greater than 0
    if (grandTotal > 0) {
      this.createJobForm.patchValue({ cost: parseFloat(grandTotal.toFixed(2)) });
    }
  }

  addServiceItem(): void {
    this.invoiceServiceItems.push({
      serviceName: '',
      quantity: 0,
      price: 0,
      total: 0
    });
  }

  removeServiceItem(index: number): void {
    this.invoiceServiceItems.splice(index, 1);
    this.calculateInvoiceTotals();
  }

  updateServiceItemTotal(index: number): void {
    const item = this.invoiceServiceItems[index];
    if (item) {
      item.total = item.quantity * item.price;
      this.calculateInvoiceTotals();
    }
  }

  async calculatePrice(): Promise<void> {
    this.calculatingPrice = true;
    this.createJobError = null;
    this.latestQuote = null;

    try {
      const charges = this.createJobForm.get('invoiceCharges');
      if (!charges) {
        return;
      }

      const accountRaw = this.createJobForm.get('account')?.value;
      const accountId = accountRaw ? Number(accountRaw) : undefined;
      const accountName = accountId
        ? this.insuranceAccounts.find(a => a.id === accountId)?.name
        : undefined;

      const pickup = String(this.createJobForm.get('pickupLocation')?.value || '').trim();
      const destination = String(this.createJobForm.get('destinationAddress')?.value || this.createJobForm.get('dropoffLocation')?.value || '').trim();

      let milesAB = parseFloat(charges.get('unloadedEnrouteMileageQuantity')?.value || '0');
      let milesBC = parseFloat(charges.get('loadedHookedMileageQuantity')?.value || '0');
      let milesCA = parseFloat(charges.get('deadHeadMileageQuantity')?.value || '0');

      if (pickup && destination) {
        const office = await firstValueFrom(this.locationService.getOfficeLocation());
        const officeRef = `${office.lat},${office.lng}`;
        const [ab, bc, ca] = await Promise.all([
          this.computeDrivingMiles(officeRef, pickup),
          this.computeDrivingMiles(pickup, destination),
          this.computeDrivingMiles(destination, officeRef)
        ]);

        this.officeToPickupMiles = ab;
        this.pickupToDestinationMiles = bc;
        this.dropoffToOfficeMiles = ca;

        milesAB = ab;
        milesBC = bc;
        milesCA = ca;

        charges.patchValue({
          unloadedEnrouteMileageQuantity: milesAB,
          loadedHookedMileageQuantity: milesBC,
          deadHeadMileageQuantity: milesCA
        });
      }

      const serviceItemsTotal = this.invoiceServiceItems.reduce((sum: number, item: any) => sum + (item.quantity * item.price), 0);
      const quote = await firstValueFrom(this.pricingService.quote({
        accountId: Number.isFinite(accountId as number) ? accountId : undefined,
        accountName: accountName || undefined,
        milesAB,
        milesBC,
        milesCA,
        extraItemsTotal: serviceItemsTotal,
        discountAmount: parseFloat(charges.get('discount')?.value || '0'),
        discountPercent: parseFloat(charges.get('discountPercent')?.value || '0') || undefined,
        taxExempt: !!charges.get('taxExempt')?.value,
        hookupFee: parseFloat(charges.get('hookupFee')?.value || '0') || undefined,
        rateAB: parseFloat(charges.get('unloadedEnrouteMileagePrice')?.value || '0') || undefined,
        rateBC: parseFloat(charges.get('loadedHookedMileagePrice')?.value || '0') || undefined,
        rateCA: parseFloat(charges.get('deadHeadMileagePrice')?.value || '0') || undefined,
        serviceChargePercent: parseFloat(charges.get('serviceChargePercent')?.value || '0') || undefined,
        taxPercent: parseFloat(charges.get('taxPercent')?.value || '0') || undefined,
        manualTotalOverride: charges.get('manualTotalOverride')?.value ?? undefined,
        manualOverrideReason: charges.get('manualOverrideReason')?.value || undefined
      }));

      this.latestQuote = quote;
      charges.patchValue({
        hookupFee: quote.hookupFee,
        unloadedEnrouteMileageQuantity: quote.milesAB,
        unloadedEnrouteMileagePrice: quote.rateAB,
        loadedHookedMileageQuantity: quote.milesBC,
        loadedHookedMileagePrice: quote.rateBC,
        deadHeadMileageQuantity: quote.milesCA,
        deadHeadMileagePrice: quote.rateCA,
        discount: quote.discountAmount,
        serviceChargePercent: quote.serviceChargePercent,
        taxPercent: quote.taxPercent,
        taxExempt: quote.taxExempt
      });
      this.createJobForm.patchValue({ cost: quote.grandTotal });
    } catch (error: any) {
      this.createJobError = error?.error?.message || 'Failed to calculate pricing. Please review account, addresses, and pricing values.';
    } finally {
      this.calculatingPrice = false;
    }
  }

  private async computeDrivingMiles(origin: string, destination: string): Promise<number> {
    const apiKey = environment.mapsApiKey?.trim();
    if (!apiKey) {
      throw new Error('Maps API key is not configured.');
    }

    const loader = new Loader({ apiKey, version: 'weekly', libraries: ['places'] });
    await loader.load();

    const ds = new google.maps.DirectionsService();
    const miles = await new Promise<number>((resolve, reject) => {
      ds.route(
        {
          origin,
          destination,
          travelMode: google.maps.TravelMode.DRIVING,
          unitSystem: google.maps.UnitSystem.IMPERIAL
        },
        (result, status) => {
          if (status === google.maps.DirectionsStatus.OK && result?.routes?.[0]) {
            let meters = 0;
            for (const leg of result.routes[0].legs) {
              meters += leg.distance?.value ?? 0;
            }
            resolve(Math.round((meters * 0.000621371) * 100) / 100);
            return;
          }
          reject(new Error('Could not compute route.'));
        }
      );
    });

    return miles;
  }

  onCreateJob(): void {
    this.createJobValidationAttempted = true;
    // Mark all fields as touched to show validation errors
    this.markAllFieldsAsTouched();
    
    // Get all validation errors
    const validationErrors = this.getValidationErrors();
    
    if (validationErrors.length > 0) {
      // Store errors as array for better display
      this.validationErrorList = validationErrors;
      this.createJobError = `Please fix ${validationErrors.length} error${validationErrors.length > 1 ? 's' : ''} below`;
      
      // Scroll to footer or top summary after a brief delay to ensure DOM is updated
      setTimeout(() => {
        const errorElement =
          document.getElementById('create-job-validation-footer') ||
          document.getElementById('validation-errors');
        if (errorElement) {
          errorElement.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        }
      }, 100);
      return;
    }
    
    this.validationErrorList = [];
    this.createJobError = null;

    this.submitting = true;
    this.createJobError = null;

    // Build request payload
    const formValue = this.createJobForm.value;
    const charges = formValue.invoiceCharges;
    
    // Calculate invoice charges
    const unloadedQty = parseFloat(charges?.unloadedEnrouteMileageQuantity || '0');
    const unloadedPrice = parseFloat(charges?.unloadedEnrouteMileagePrice || '0');
    const loadedQty = parseFloat(charges?.loadedHookedMileageQuantity || '0');
    const loadedPrice = parseFloat(charges?.loadedHookedMileagePrice || '0');
    const deadQty = parseFloat(charges?.deadHeadMileageQuantity || '0');
    const deadPrice = parseFloat(charges?.deadHeadMileagePrice || '0');
    const hookupFee = parseFloat(charges?.hookupFee || '0');
    const discount = parseFloat(charges?.discount || '0');
    const discountPercent = parseFloat(charges?.discountPercent || '0');
    const serviceChargePercent = parseFloat(charges?.serviceChargePercent || '0');
    const taxPercent = parseFloat(charges?.taxPercent || '0');
    
    const serviceItemsTotal = this.invoiceServiceItems.reduce((sum: number, item: any) => 
      sum + (item.quantity * item.price), 0);
    const subtotal = hookupFee + (unloadedQty * unloadedPrice) + (loadedQty * loadedPrice) + (deadQty * deadPrice) + serviceItemsTotal;
    const effectiveDiscount = discount > 0 ? discount : (subtotal * (discountPercent / 100));
    const afterDiscount = Math.max(0, subtotal - effectiveDiscount);
    const serviceChargeAmount = afterDiscount * (serviceChargePercent / 100);
    const taxableAmount = afterDiscount + serviceChargeAmount;
    const taxes = charges?.taxExempt ? 0 : taxableAmount * (taxPercent / 100);
    const grandTotal = taxableAmount + taxes;
    
    const jobData: CreateJobRequest = {
      cost: parseFloat(formValue.cost) || grandTotal,
      serviceType: formValue.serviceType,
      pickupLocation: formValue.pickupLocation,
      dropoffLocation: formValue.dropoffLocation || formValue.destinationAddress || undefined,
      notes: formValue.notes || undefined,
      // Call/Job Type
      callType: formValue.callType,
      scheduledDate: formValue.scheduledDate || undefined,
      scheduledTime: formValue.scheduledTime || undefined,
      // Company & Account
      companyName: formValue.companyName,
      account: formValue.account || undefined,
      companyOverride: formValue.companyOverride || undefined,
      // Contact Information
      contactName: formValue.contactName || undefined,
      contactPhoneNumber: formValue.contactPhoneNumber || undefined,
      // Location
      destinationAddress: formValue.destinationAddress || formValue.dropoffLocation || undefined,
      // Job Details
      priority: formValue.priority,
      eta: formValue.eta || undefined,
      // Assignment
      driverId: formValue.driverId || undefined,
      truckId: formValue.truckId || undefined,
      // Notes
      billingNotes: formValue.billingNotes || undefined,
      includeBillingNotesOnReceipt: formValue.includeBillingNotesOnReceipt || false,
      // Invoice Charges
      invoiceCharges: {
        unloadedEnrouteMileage: unloadedQty > 0 || unloadedPrice > 0 ? {
          quantity: unloadedQty,
          price: unloadedPrice,
          total: unloadedQty * unloadedPrice
        } : undefined,
        loadedHookedMileage: loadedQty > 0 || loadedPrice > 0 ? {
          quantity: loadedQty,
          price: loadedPrice,
          total: loadedQty * loadedPrice
        } : undefined,
        deadHeadMileage: deadQty > 0 || deadPrice > 0 ? {
          quantity: deadQty,
          price: deadPrice,
          total: deadQty * deadPrice
        } : undefined,
        serviceItems: this.invoiceServiceItems.length > 0 ? this.invoiceServiceItems.map((item: any) => ({
          serviceName: item.serviceName,
          quantity: item.quantity,
          price: item.price,
          total: item.quantity * item.price
        })) : undefined,
        hookupFee: hookupFee > 0 ? hookupFee : undefined,
        discount: effectiveDiscount > 0 ? effectiveDiscount : undefined,
        discountPercent: discountPercent > 0 ? discountPercent : undefined,
        serviceChargePercent: serviceChargePercent > 0 ? serviceChargePercent : undefined,
        serviceChargeAmount: serviceChargeAmount > 0 ? serviceChargeAmount : undefined,
        taxPercent: taxPercent > 0 ? taxPercent : undefined,
        taxExempt: charges?.taxExempt || false,
        subtotal: subtotal,
        taxes: taxes,
        grandTotal: grandTotal,
        manualTotalOverride: charges?.manualTotalOverride || undefined,
        manualOverrideReason: charges?.manualOverrideReason || undefined
      }
    };

    // Add client data
    if (this.useExistingClient) {
      jobData.clientId = formValue.client.clientId;
    } else {
      jobData.client = {
        phoneNumber: formValue.client.clientPhoneNumber, // Required - main key
        fullName: formValue.client.clientFullName,
        email: formValue.client.clientEmail || undefined, // Optional
        contactName: formValue.contactName || undefined
      };
    }

    // Add vehicle data
    if (this.useExistingVehicle) {
      jobData.vehicleId = parseInt(formValue.vehicle.vehicleId);
    } else {
      jobData.vehicle = {
        vin: formValue.vehicle.vehicleVin,
        make: formValue.vehicle.vehicleMake,
        model: formValue.vehicle.vehicleModel,
        year: parseInt(formValue.vehicle.vehicleYear),
        color: formValue.vehicle.vehicleColor || undefined,
        licensePlate: formValue.vehicle.licensePlate || undefined,
        licenseState: formValue.vehicle.licenseState || undefined,
        driveType: formValue.vehicle.driveType || undefined,
        vehicleType: formValue.vehicle.vehicleType || undefined,
        odometer: formValue.vehicle.odometer ? parseInt(formValue.vehicle.odometer) : undefined,
        drivable: formValue.vehicle.drivable || undefined,
        haveKeys: formValue.vehicle.haveKeys || false,
        keyLocation: formValue.vehicle.keyLocation || undefined
      };
    }

    this.jobService.createJob(jobData)
      .pipe(
        finalize(() => this.submitting = false),
        catchError(error => {
          this.createJobError = error.error?.message || 'Failed to create job';
          return of(null);
        })
      )
      .subscribe(job => {
        if (job) {
          this.closeCreateJobModal();
          this.loadJobs();
          this.loadVehicles(); // Reload vehicles in case new one was created
          this.loadClients(); // Reload clients in case new one was created
        }
      });
  }

  getNextStatuses(job: Job): Job['status'][] {
    const currentIndex = this.statusProgression.indexOf(job.status);
    if (currentIndex === -1) {
      return [];
    }
    return this.statusProgression.slice(currentIndex + 1);
  }

  openUpdateStatusModal(job: Job): void {
    const nextStatuses = this.getNextStatuses(job);
    if (!nextStatuses.length) {
      this.error = 'This job is already at the final status.';
      return;
    }

    this.selectedJob = job;
    this.updateStatusControl.setValue(nextStatuses[0]);
    this.showUpdateStatusModal = true;
  }

  closeUpdateStatusModal(): void {
    this.showUpdateStatusModal = false;
    this.selectedJob = null;
    this.updateStatusControl.reset('');
    this.error = null;
  }

  onUpdateStatus(): void {
    if (!this.selectedJob || this.updateStatusControl.invalid) {
      this.updateStatusControl.markAsTouched();
      return;
    }

    this.submitting = true;
    this.error = null;

    const newStatus = this.updateStatusControl.value as UpdateJobStatusRequest['status'];
    this.jobService.updateJobStatus(this.selectedJob.id, { status: newStatus })
      .pipe(
        finalize(() => this.submitting = false),
        catchError(error => {
          this.error = error.error?.message || 'Failed to update job status';
          return of(null);
        })
      )
      .subscribe(result => {
        if (result !== null) {
          this.closeUpdateStatusModal();
          this.loadJobs();
        }
      });
  }

  openAssignDriverModal(job: Job): void {
    if (job.status !== 'Pending') {
      this.error = 'Only pending jobs can be assigned to drivers';
      return;
    }
    
    this.selectedJob = job;
    this.showAssignDriverModal = true;
    this.assignDriverForm.patchValue({ driverId: job.driverId || '' });
  }

  closeAssignDriverModal(): void {
    this.showAssignDriverModal = false;
    this.selectedJob = null;
    this.assignDriverForm.reset();
    this.error = null;
  }

  onAssignDriver(): void {
    if (this.assignDriverForm.invalid || !this.selectedJob) {
      this.assignDriverForm.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.error = null;

    const assignData: AssignDriverRequest = {
      driverId: this.assignDriverForm.value.driverId
    };

    this.jobService.assignDriver(this.selectedJob.id, assignData.driverId)
      .pipe(
        finalize(() => this.submitting = false),
        catchError(error => {
          this.error = error.error?.message || 'Failed to assign driver';
          return of(null);
        })
      )
      .subscribe((result: AssignDriverResponse | null) => {
        if (result) {
          this.pushNotificationWarning =
            result.notificationSent || !result.notificationMessage
              ? null
              : result.notificationMessage;
          this.closeAssignDriverModal();
          this.loadJobs();
        }
      });
  }

  clearPushNotificationWarning(): void {
    this.pushNotificationWarning = null;
  }

  openJobDetails(job: Job): void {
    this.selectedJob = job;
    this.showJobDetailsModal = true;
  }

  closeJobDetails(): void {
    this.showJobDetailsModal = false;
    this.selectedJob = null;
  }

  getStatusBadgeClass(status: string): string {
    const classes: Record<string, string> = {
      'Pending': 'bg-yellow-100 text-yellow-800',
      'Assigned': 'bg-blue-100 text-blue-800',
      'OnRoute': 'bg-purple-100 text-purple-800',
      'InProgress': 'bg-indigo-100 text-indigo-800',
      'ReadyToRelease': 'bg-green-100 text-green-800',
      'Completed': 'bg-gray-100 text-gray-800'
    };
    return classes[status] || 'bg-gray-100 text-gray-800';
  }

  getVehicleDisplay(vehicle: Vehicle | undefined): string {
    if (!vehicle) return 'N/A';
    return `${vehicle.year} ${vehicle.make} ${vehicle.model} (${vehicle.color})`;
  }

  formatDate(dateString: string | null | undefined): string {
    if (!dateString) return 'N/A';
    try {
      return new Date(dateString).toLocaleString();
    } catch {
      return 'N/A';
    }
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  }

  // Helper methods for template calculations
  parseFloat(value: any): number {
    return parseFloat(value) || 0;
  }

  getSubtotal(): number {
    const unloadedQty = parseFloat(this.createJobForm.get('invoiceCharges.unloadedEnrouteMileageQuantity')?.value || '0');
    const unloadedPrice = parseFloat(this.createJobForm.get('invoiceCharges.unloadedEnrouteMileagePrice')?.value || '0');
    const loadedQty = parseFloat(this.createJobForm.get('invoiceCharges.loadedHookedMileageQuantity')?.value || '0');
    const loadedPrice = parseFloat(this.createJobForm.get('invoiceCharges.loadedHookedMileagePrice')?.value || '0');
    const deadQty = parseFloat(this.createJobForm.get('invoiceCharges.deadHeadMileageQuantity')?.value || '0');
    const deadPrice = parseFloat(this.createJobForm.get('invoiceCharges.deadHeadMileagePrice')?.value || '0');
    const hookupFee = parseFloat(this.createJobForm.get('invoiceCharges.hookupFee')?.value || '0');
    const serviceItemsTotal = this.invoiceServiceItems.reduce((sum, item) => sum + (item.quantity * item.price), 0);
    return hookupFee + (unloadedQty * unloadedPrice) + (loadedQty * loadedPrice) + (deadQty * deadPrice) + serviceItemsTotal;
  }

  getTaxes(): number {
    const taxExempt = this.createJobForm.get('invoiceCharges.taxExempt')?.value || false;
    if (taxExempt) return 0;
    const taxable = this.getTaxableAmount();
    const taxPercent = parseFloat(this.createJobForm.get('invoiceCharges.taxPercent')?.value || '0');
    return taxable * (taxPercent / 100);
  }

  getTaxableAmount(): number {
    const subtotal = this.getSubtotal();
    const discountFlat = parseFloat(this.createJobForm.get('invoiceCharges.discount')?.value || '0');
    const discountPercent = parseFloat(this.createJobForm.get('invoiceCharges.discountPercent')?.value || '0');
    const discount = discountFlat > 0 ? discountFlat : (subtotal * (discountPercent / 100));
    const serviceChargePercent = parseFloat(this.createJobForm.get('invoiceCharges.serviceChargePercent')?.value || '0');
    const afterDiscount = Math.max(0, subtotal - discount);
    const serviceCharge = afterDiscount * (serviceChargePercent / 100);
    return afterDiscount + serviceCharge;
  }

  getGrandTotal(): number {
    const taxes = this.getTaxes();
    return this.getTaxableAmount() + taxes;
  }

  /** SuperAdmin, Administrator, Dispatcher — matches API `payments/create-payment-link`. */
  canCreatePaymentLink(): boolean {
    const u = this.authService.getCurrentUser();
    if (!u) {
      return false;
    }
    // localStorage user may deserialize roleId as string — use Number() for strict checks
    const rid = Number(u.roleId);
    return (
      rid === RoleId.SuperAdmin ||
      rid === RoleId.Admin ||
      rid === RoleId.Dispatcher
    );
  }

  isJobEligibleForPaymentLink(job: Job): boolean {
    if (!this.canCreatePaymentLink()) {
      return false;
    }
    const cost = Number(job.cost);
    if (!Number.isFinite(cost) || cost <= 0) {
      return false;
    }
    const s = String(job.status ?? '');
    if (s === 'Completed' || s === 'Cancelled') {
      return false;
    }
    const ps = String(job.paymentStatus ?? 'Unpaid');
    if (['Paid', 'Refunded', 'PartiallyRefunded'].includes(ps)) {
      return false;
    }
    return true;
  }

  /** Display label for job payment state (API Job.PaymentStatus). */
  jobPaymentStatusLabel(job: Job): string {
    return job.paymentStatus?.trim() || 'Unpaid';
  }

  getPaymentStatusBadgeClass(paymentStatus: string | undefined): string {
    const s = (paymentStatus || 'Unpaid').toLowerCase();
    if (s === 'paid') {
      return 'bg-emerald-100 text-emerald-800';
    }
    if (s === 'pending' || s === 'pendingcash') {
      return 'bg-amber-100 text-amber-800';
    }
    if (s === 'failed' || s === 'cancelled') {
      return 'bg-red-100 text-red-800';
    }
    if (s === 'authorized' || s === 'capturepending' || s === 'underreview') {
      return 'bg-sky-100 text-sky-800';
    }
    if (s === 'refunded' || s === 'partiallyrefunded') {
      return 'bg-violet-100 text-violet-800';
    }
    return 'bg-gray-100 text-gray-700';
  }

  paymentLinkSuccessUrl(): string {
    const origin = window.location.origin;
    if (window.location.pathname.startsWith('/admin')) {
      return `${origin}/admin/payments`;
    }
    return `${origin}/dispatcher/payments`;
  }

  openPaymentLinkModal(job: Job): void {
    this.paymentLinkJob = job;
    this.paymentLinkUrl = null;
    this.paymentLinkError = null;
    this.paymentLinkCopied = false;
    this.showPaymentLinkModal = true;
  }

  closePaymentLinkModal(): void {
    this.showPaymentLinkModal = false;
    this.paymentLinkJob = null;
    this.paymentLinkUrl = null;
    this.paymentLinkError = null;
    this.paymentLinkCopied = false;
  }

  createPaymentLinkForJob(): void {
    if (!this.paymentLinkJob) {
      return;
    }
    this.paymentLinkSubmitting = true;
    this.paymentLinkError = null;
    this.paymentLinkUrl = null;
    this.paymentLinkCopied = false;

    this.paymentService
      .createStripePaymentLink({
        jobId: this.paymentLinkJob.id,
        amount: this.paymentLinkJob.cost,
        successUrl: this.paymentLinkSuccessUrl()
      })
      .pipe(
        finalize(() => (this.paymentLinkSubmitting = false)),
        catchError((error) => {
          this.paymentLinkError =
            error.error?.message || error.error?.error || 'Failed to create payment link. Is Stripe enabled in Settings?';
          return of(null);
        })
      )
      .subscribe((res) => {
        if (res?.url) {
          this.paymentLinkUrl = res.url;
          this.loadJobs();
        }
      });
  }

  openCardGatewayForJob(job: Job): void {
    if (!this.isJobEligibleForPaymentLink(job) || this.quickPaySubmittingJobId !== null) {
      return;
    }

    this.quickPaySubmittingJobId = job.id;
    this.error = null;

    this.paymentService
      .createStripePaymentLink({
        jobId: job.id,
        amount: job.cost,
        successUrl: this.paymentLinkSuccessUrl()
      })
      .pipe(
        finalize(() => (this.quickPaySubmittingJobId = null)),
        catchError((error) => {
          this.error =
            error.error?.message ||
            error.error?.error ||
            'Failed to open card payment gateway. Is Stripe enabled in Settings?';
          return of(null);
        })
      )
      .subscribe((res) => {
        if (res?.url) {
          window.open(res.url, '_blank', 'noopener,noreferrer');
          this.loadJobs();
        }
      });
  }

  copyPaymentLinkToClipboard(): void {
    if (!this.paymentLinkUrl) {
      return;
    }
    navigator.clipboard.writeText(this.paymentLinkUrl).then(() => {
      this.paymentLinkCopied = true;
      setTimeout(() => (this.paymentLinkCopied = false), 2500);
    });
  }
}

