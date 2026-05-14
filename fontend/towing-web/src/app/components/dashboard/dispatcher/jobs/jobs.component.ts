import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  ElementRef,
  HostListener,
  NgZone,
  OnDestroy,
  OnInit,
  ViewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators, FormControl, AbstractControl } from '@angular/forms';
import { HttpParams } from '@angular/common/http';
import {
  catchError,
  debounceTime,
  distinctUntilChanged,
  filter,
  finalize,
  map,
  startWith,
  switchMap
} from 'rxjs/operators';
import {
  combineLatest,
  EMPTY,
  firstValueFrom,
  from,
  merge,
  Observable,
  of,
  Subject,
  Subscription
} from 'rxjs';
import {
  VehicleCatalogMakeItem,
  VehicleCatalogModelItem,
  VehicleCatalogService
} from '../../../../services/vehicle-catalog.service';
import { Loader } from '@googlemaps/js-api-loader';
import {
  JobService,
  Job,
  CreateJobRequest,
  AssignDriverRequest,
  AssignDriverResponse,
  VehicleData,
  ClientData,
  UpdateJobStatusRequest,
  OverrideJobPriceRequest,
  JOB_STATUS,
  JOB_STATUS_PIPELINE,
  JOB_STATUS_ORDER,
  JOB_STATUS_LABELS,
  JobStatus,
  formatJobStatusLabel,
  JOB_BILLING_PAYMENT_MODE,
  UpdateJobBillingPaymentRequest
} from '../../../../services/job.service';
import { TruckService, TruckListItem } from '../../../../services/truck.service';
import { VehicleService, Vehicle } from '../../../../services/vehicle.service';
import { ApiService } from '../../../../services/api.service';
import { AccountsService } from '../../../../services/accounts.service';
import { ServicePricingService } from '../../../../services/service-pricing.service';
import { AuthService } from '../../../../services/auth.service';
import { PaymentService } from '../../../../services/payment.service';
import { InsuranceAccount } from '../../../../models/insurance-account.model';
import { InsuranceAccountServiceRate } from '../../../../models/insurance-account-service-rate.model';
import { ServicePricingProfile } from '../../../../models/service-pricing.model';
import { User } from '../../../../models/user.model';
import { RoleId } from '../../../../constants/user-roles.constants';
import { PlacesAutocompleteDirective } from '../../../../directives/places-autocomplete.directive';
import { LocationPickerComponent } from '../../../shared/location-picker/location-picker.component';
import { QuotePrintComponent, QuotePrintData } from '../../shared/quote-print/quote-print.component';
import { LocationService } from '../../../../services/location.service';
import { PricingService, PricingQuoteResponse } from '../../../../services/pricing.service';
import { QuoteService } from '../../../../services/quote.service';
import { SettingsService } from '../../../../services/settings.service';
import { environment } from '../../../../../environments/environment';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { Router, ActivatedRoute } from '@angular/router';

const JOBS_TABLE_COL_COUNT = 17;
const JOBS_STICKY_STORAGE_KEY = 'dispatcherJobsTableStickyColumns';

/** Per column: off, stick to left when scrolling, or stick to right when scrolling. */
type JobTableStickyPin = 'off' | 'left' | 'right';

function defaultJobsTableStickyConfig(): JobTableStickyPin[] {
  const d = new Array<JobTableStickyPin>(JOBS_TABLE_COL_COUNT).fill('off');
  d[0] = 'left';
  d[1] = 'left';
  d[16] = 'right';
  return d;
}

function migrateJobsTableStickyFromBooleans(parsed: boolean[]): JobTableStickyPin[] {
  return parsed.map((b, i) => {
    if (!b) {
      return 'off';
    }
    return i === JOBS_TABLE_COL_COUNT - 1 ? 'right' : 'left';
  });
}

/** Single row in the quote line-items table (modal + PDF). */
interface QuoteLineItem {
  description: string;
  subtitle?: string;
  quantity: string;
  unitPrice: string;
  amount: string;
}

/** Structured quote summary for the review modal and PDF export */
interface QuoteReviewSummary {
  quoteRef: string;
  quoteDateDisplay: string;
  validUntilDisplay: string;
  fromLines: string[];
  toLines: string[];
  lineItems: QuoteLineItem[];
  /** Pre-tax total (after discount + service charge), matches API `taxableAmount` when server quote exists */
  totalsSubtotal: string;
  totalsTaxLabel: string;
  totalsTax: string;
  mileage: { label: string; value: string }[];
  client: { label: string; value: string }[];
  pickup: string;
  destination: string;
  serviceType: string;
  vehicleLabel: string;
  totalAmount: string;
  notesPreview: string;
  footerNote: string;
  /** Extended fields for the HTML quote preview */
  companyName: string;
  companyAddress: string;
  companyPhone: string;
  companyEmail: string;
  companyWebsite: string;
  clientName: string;
  clientPhone: string;
  /** Single line for quote card, e.g. `moha (+123456789)` */
  clientDisplayLine: string;
  serviceDateDisplay: string;
  truckTypeLabel: string;
  loadedMileageDisplay: string;
  taxPercent: string;
}

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
    LocationPickerComponent,
    QuotePrintComponent
  ],
  templateUrl: './jobs.component.html',
  styleUrls: ['./jobs.component.scss']
})
export class JobsComponent implements OnInit, OnDestroy, AfterViewInit {
  /** Expose for template (`job.status === JOB_STATUS.Waiting`). */
  readonly JOB_STATUS = JOB_STATUS;
  readonly JOB_BILLING_PAYMENT_MODE = JOB_BILLING_PAYMENT_MODE;
  /** Full lifecycle order — all statuses shown in job details (e.g. new job = Waiting current, rest upcoming). */
  readonly JOB_STATUS_ORDER_ALL = JOB_STATUS_ORDER;
  jobs: Job[] = [];
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

  /** Admin-only: set job price after creation (job details modal). */
  priceOverrideCost: number | null = null;
  priceOverrideReason = '';
  priceOverrideSubmitting = false;
  priceOverrideError: string | null = null;
  /** When saving admin price override, allow driver to see commission estimate. */
  priceOverrideCommissionVisible = false;

  /** Job details modal — billing / payment arrangement (synced in openJobDetails). */
  billingBillingPaymentMode: string = JOB_BILLING_PAYMENT_MODE.Standard;
  billingInsuranceCoveredAmount: number | null = null;
  billingClientCoveredAmount: number | null = null;
  billingInsurancePortionBilled = false;
  billingClientPortionPaid = false;
  billingDriverCashCollectedAmount: number | null = null;
  billingPayrollDeductionAmount: number | null = null;
  billingPayrollDeductionRecorded = false;
  billingSubmitting = false;
  billingError: string | null = null;
  
  // Forms
  createJobForm: FormGroup;
  assignDriverForm: FormGroup;
  updateStatusControl = new FormControl<string>('', { nonNullable: true, validators: [Validators.required] });
  submitting = false;
  
  // Filters
  statusFilter: string = '';
  searchTerm: string = '';

  pageNumber = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;
  hasPreviousPage = false;
  hasNextPage = false;
  /** @see users table */
  Math = Math;
  private searchDebounceTimer: ReturnType<typeof setTimeout> | undefined;

  /**
   * Drives `loadJobs` through a single `switchMap` so any in-flight paged
   * request is cancelled when filters/pagination change. Avoids the prior
   * race where slow responses could overwrite newer ones.
   */
  private readonly loadJobsRequest$ = new Subject<void>();
  private loadJobsRequestSub?: Subscription;

  // Data for dropdowns
  vehicles: Vehicle[] = [];
  filteredVehicles: Vehicle[] = [];
  drivers: User[] = [];
  availableDrivers: User[] = [];

  /** Lazy-load guards — dropdown data is only fetched the first time the
   * relevant modal opens, instead of competing with the initial jobs request. */
  private vehiclesLoaded = false;
  private driversLoaded = false;
  private trucksLoaded = false;
  private servicePricingProfilesLoaded = false;
  private dispatchOfficeLoaded = false;
  private settingsLoaded = false;

  /** Searchable client combobox (create job → existing client); data from `GET users/clients`. */
  createJobClientSearch = '';
  createJobClientSelectOpen = false;
  createJobClientSearchResults: User[] = [];
  createJobClientSearchLoading = false;
  private createJobSelectedClient: User | null = null;
  private readonly createJobClientInstant$ = new Subject<string>();
  private readonly createJobClientDebounced$ = new Subject<string>();
  private createJobClientFetchSub?: Subscription;
  @ViewChild('createJobClientSelectRoot') createJobClientSelectRoot?: ElementRef<HTMLElement>;
  
  // Selection modes
  useExistingClient: boolean = true;
  useExistingVehicle: boolean = true;
  
  // Validation error list
  validationErrorList: string[] = [];

  /** After user clicks Create Job once, show inline errors even if fields were not touched */
  createJobValidationAttempted = false;

  // Create job modal tabs
  activeCreateJobTab: 'details' | 'payment' = 'details';
  
  // Status options (all values from JOB_STATUS / JOB_STATUS_LABELS in job.service)
  statusOptions = [
    { value: '', label: 'All Statuses' },
    ...JOB_STATUS_ORDER.map((s) => ({ value: s, label: JOB_STATUS_LABELS[s] }))
  ];
  
  statusProgression: JobStatus[] = [...JOB_STATUS_PIPELINE];
  
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
  
  trucks: TruckListItem[] = [];
  trucksLoading = false;

  detailTruckId: number | null = null;
  detailTruckSaving = false;
  detailTruckError: string | null = null;
  
  // Service items for invoice charges
  serviceItems: any[] = [];
  
  // Invoice charges form arrays
  invoiceServiceItems: any[] = [];

  /** Insurance accounts for job Account dropdown (active only) */
  insuranceAccounts: InsuranceAccount[] = [];
  insuranceAccountsLoading = false;
  servicePricingProfiles: ServicePricingProfile[] = [];
  servicePricingProfilesLoading = false;

  /** Per-account × service rows for the selected insurance account (job create). */
  createJobAccountServiceRates: InsuranceAccountServiceRate[] = [];
  private accountRatesSub?: Subscription;

  /** System default hookup (Admin → Settings); used for legacy pricing when no service catalog. */
  defaultHookupFromSettings = 0;

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

  /**
   * Auto price recalculation pipeline.
   *
   * `priceRecalcRequested$` is also pushed to from places that mutate inputs
   * outside the reactive form (e.g. the `invoiceServiceItems` array). It is
   * merged with `valueChanges` of the pricing-relevant form controls and
   * debounced before calling {@link calculatePrice}.
   *
   * `priceRecalcGeneration` is used as a stale-response guard so that an older
   * in-flight quote (Maps + API) cannot overwrite a newer one if the user
   * keeps editing while the request is pending.
   */
  private readonly priceRecalcRequested$ = new Subject<void>();
  private priceRecalcSub?: Subscription;
  private priceRecalcGeneration = 0;

  /** Quote call type: review summary without creating a job */
  showQuoteReviewModal = false;
  /** Editable recipient for SMS (defaults from contact / client phone) */
  quoteSmsPhone = '';

  /**
   * Email-quote confirmation modal — stacked above the quote review modal.
   * The dispatcher confirms recipient + subject + message; on confirm we render
   * the same PDF the preview shows and send it via Postmark using settings credentials.
   */
  showQuoteEmailModal = false;
  quoteEmailRecipient = '';
  quoteEmailSubject = '';
  quoteEmailMessage = '';
  quoteEmailSending = false;
  quoteEmailError: string | null = null;
  /** Email address shown in the success banner after a successful send (cleared on close). */
  quoteEmailSuccess: string | null = null;
  /** Snapshot when opening quote review (mileage, client, locations, vehicle, total) */
  quoteReviewSummary: QuoteReviewSummary | null = null;
  /** Object URL for in-modal PDF preview (revoked on close). */
  private quotePdfBlobUrl: string | null = null;
  quotePdfSafeUrl: SafeResourceUrl | null = null;
  quotePdfGenerating = false;
  /** Server-side Playwright PDF download in progress */
  quotePdfDownloading = false;
  quotePdfError: string | null = null;
  /**
   * TESTING: `true` = quote modal shows live HTML only (iframe PDF reviewer skipped). Set `false` when done styling / for production.
   */
  quoteReviewHtmlPreviewOnly = true;
  private routeDistanceGeneration = 0;
  /** Cached PNG data URL extracted from `public/images/logo.svg` (for jsPDF). */
  private quoteLogoPngDataUrl: string | null | undefined;
  private pickupDestinationDistanceSub?: Subscription;
  private catalogMakeSearchSub?: Subscription;
  private catalogModelSearchSub?: Subscription;
  private readonly makeSearch$ = new Subject<string>();
  private readonly modelSearch$ = new Subject<{ makeId: number; q: string }>();

  /** Per-column sticky (pin) — horizontal scroll. Persisted in localStorage. */
  jobsTableStickyMode: JobTableStickyPin[] = defaultJobsTableStickyConfig();

  /**
   * Cached `[ngStyle]` objects per column for the dispatcher table. These are
   * recomputed only when sticky mode changes or column widths shift in
   * `measureJobsStickyOffsets`, so the per-row template bindings stay
   * referentially stable across change-detection cycles (no per-cell
   * allocations and no needless DOM writes through `KeyValueDiffer`).
   */
  private cachedThStyles: ReadonlyArray<Record<string, string>> = new Array(JOBS_TABLE_COL_COUNT).fill({});
  private cachedTdStyles: ReadonlyArray<Record<string, string>> = new Array(JOBS_TABLE_COL_COUNT).fill({});

  /** Measured cumulative `left` / `right` (px) for pinned columns. */
  private stickyLeftPx: (number | undefined)[] = new Array(JOBS_TABLE_COL_COUNT).fill(undefined);
  private stickyRightPx: (number | undefined)[] = new Array(JOBS_TABLE_COL_COUNT).fill(undefined);

  @ViewChild('jobsTableWrap') jobsTableWrapRef?: ElementRef<HTMLElement>;
  @ViewChild('jobsTableHeaderRow') jobsTableHeaderRowRef?: ElementRef<HTMLTableRowElement>;
  @ViewChild('quotePrintRef') quotePrintRef?: QuotePrintComponent;

  private jobsStickyResizeObserver?: ResizeObserver;
  private jobsStickyMeasureRaf = 0;

  /** DB-backed catalog (sync from Admin → Settings). Searchable make/model pickers. */
  catalogMakeSearchDraft = '';
  catalogModelSearchDraft = '';
  /** When user picks from catalog, blur matches this name to keep catalog model search; otherwise SSOT is plain text on `vehicleMake`. */
  private lastPickedCatalogMakeName: string | null = null;
  catalogMakeSuggestions: VehicleCatalogMakeItem[] = [];
  catalogModelSuggestions: VehicleCatalogModelItem[] = [];
  catalogMakeSearchLoading = false;
  catalogModelSearchLoading = false;
  catalogEmpty = false;
  catalogModelEmpty = false;
  showMakeDropdown = false;
  showModelDropdown = false;

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
    private quoteService: QuoteService,
    private settingsService: SettingsService,
    private vehicleCatalogService: VehicleCatalogService,
    private truckService: TruckService,
    private router: Router,
    private route: ActivatedRoute,
    private ngZone: NgZone,
    private cdr: ChangeDetectorRef,
    private fb: FormBuilder,
    private sanitizer: DomSanitizer
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
      vehicleCatalogMakeId: [null as number | null],
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
      truckId: [null as number | null],
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

    this.accountRatesSub = this.createJobForm
      .get('account')
      ?.valueChanges.pipe(
        switchMap((raw) => {
          const id =
            raw !== null && raw !== undefined && String(raw).trim() !== '' ? Number(raw) : NaN;
          if (!Number.isFinite(id) || id <= 0) {
            return of([] as InsuranceAccountServiceRate[]);
          }
          return this.accountsService.listServiceRates(id).pipe(
            catchError(() => of([] as InsuranceAccountServiceRate[]))
          );
        })
      )
      .subscribe((rates) => {
        this.createJobAccountServiceRates = rates ?? [];
        this.applySelectedServicePricing();
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

  /**
   * Subscribes once (in `ngOnInit`) to every form input that affects the
   * server price quote and re-runs {@link calculatePrice} after the user
   * stops editing for a short window. Also exposes `priceRecalcRequested$`
   * for non-form inputs (e.g. `invoiceServiceItems`) to feed the same
   * pipeline via {@link requestPriceRecalc}.
   *
   * - `debounceTime` collapses rapid successive edits into one API call.
   * - `switchMap` cancels the previous pending recalc when a new one starts.
   * - The filter gates the pipeline to "modal open AND something pricing-meaningful
   *   exists" so we don't fire empty quotes during open/reset.
   * - {@link calculatePrice} patches the form with `{ emitEvent: false }`,
   *   which prevents the patches from re-triggering this pipeline.
   */
  private setupAutoPriceRecalc(): void {
    const charges = this.createJobForm.get('invoiceCharges');
    const pricingInputs$ = merge(
      this.priceRecalcRequested$,
      this.createJobForm.get('account')!.valueChanges.pipe(map(() => undefined)),
      this.createJobForm.get('serviceType')!.valueChanges.pipe(map(() => undefined)),
      this.createJobForm.get('pickupLocation')!.valueChanges.pipe(map(() => undefined)),
      this.createJobForm.get('destinationAddress')!.valueChanges.pipe(map(() => undefined)),
      charges ? charges.valueChanges.pipe(map(() => undefined)) : EMPTY
    );

    this.priceRecalcSub = pricingInputs$
      .pipe(
        filter(() => this.showCreateJobModal && this.hasPricingInput()),
        debounceTime(600),
        switchMap(() => from(this.calculatePrice()))
      )
      .subscribe();
  }

  /** Push an auto-recalc through the same debounced pipeline (used by non-form inputs). */
  private requestPriceRecalc(): void {
    this.priceRecalcRequested$.next();
  }

  /**
   * Avoid spamming the server quote API for an empty form. We require at
   * least one of: a service type, an account, or any extra service item.
   */
  private hasPricingInput(): boolean {
    const serviceType = String(this.createJobForm.get('serviceType')?.value ?? '').trim();
    const account = String(this.createJobForm.get('account')?.value ?? '').trim();
    const hasItems = this.invoiceServiceItems.length > 0;
    return !!serviceType || !!account || hasItems;
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
    this.applyJobsRouteQueryParams();
    this.loadJobsTableStickyConfig();

    // Single-subscriber pipeline that cancels any in-flight jobs request when
    // a new one is triggered (filter change, pagination, post-mutation refresh).
    this.loadJobsRequestSub = this.loadJobsRequest$
      .pipe(
        switchMap(() => {
          this.loading = true;
          this.error = null;
          this.cdr.markForCheck();
          return this.jobService
            .getJobsPaged({
              pageNumber: this.pageNumber,
              pageSize: this.pageSize,
              status: this.statusFilter || undefined,
              search: this.searchTerm.trim() || undefined
            })
            .pipe(
              catchError((error) => {
                this.error = error.error?.message || 'Failed to load jobs';
                this.jobs = [];
                this.totalCount = 0;
                this.totalPages = 0;
                this.hasPreviousPage = false;
                this.hasNextPage = false;
                return of({
                  data: [] as Job[],
                  pageNumber: 1,
                  pageSize: this.pageSize,
                  totalCount: 0,
                  totalPages: 0,
                  hasPreviousPage: false,
                  hasNextPage: false
                });
              }),
              finalize(() => {
                this.loading = false;
                setTimeout(() => {
                  this.setupJobsStickyResizeObserver();
                  this.scheduleJobsStickyMeasure();
                }, 0);
              })
            );
        })
      )
      .subscribe((response) => {
        this.pageNumber = response.pageNumber;
        this.pageSize = response.pageSize;
        this.totalCount = response.totalCount;
        this.totalPages = response.totalPages;
        this.hasPreviousPage = response.hasPreviousPage;
        this.hasNextPage = response.hasNextPage;
        this.jobs = response.data;
        this.cdr.markForCheck();
      });

    this.loadJobs();

    // Dropdown data, dispatch office, and system settings are deferred until
    // the modals that need them open — this stops them from competing with
    // /api/jobs for the browser's per-origin connection slots on first paint.
    this.observeSelectedClientChanges();

    this.createJobClientFetchSub = merge(
      this.createJobClientInstant$,
      this.createJobClientDebounced$.pipe(debounceTime(300), distinctUntilChanged())
    )
      .pipe(
        distinctUntilChanged(),
        switchMap((q): Observable<User[]> => this.runCreateJobClientFetch(q))
      )
      .subscribe((rows: User[]) => {
        this.createJobClientSearchResults = rows;
        this.cdr.markForCheck();
      });

    this.catalogMakeSearchSub = this.makeSearch$
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        switchMap((q) =>
          this.vehicleCatalogService.searchMakes(q, 1, 30).pipe(
            catchError(() =>
              of({
                items: [] as VehicleCatalogMakeItem[],
                totalCount: 0,
                page: 1,
                pageSize: 30,
                totalPages: 0,
                catalogEmpty: false
              })
            )
          )
        )
      )
      .subscribe((r) => {
        this.catalogMakeSuggestions = r.items;
        this.catalogEmpty = r.catalogEmpty;
        this.catalogMakeSearchLoading = false;
        this.cdr.markForCheck();
      });

    this.catalogModelSearchSub = this.modelSearch$
      .pipe(
        debounceTime(300),
        distinctUntilChanged((a, b) => a.makeId === b.makeId && a.q === b.q),
        switchMap(({ makeId, q }) =>
          this.vehicleCatalogService.searchModels(makeId, q, 1, 30).pipe(
            catchError(() =>
              of({
                items: [] as VehicleCatalogModelItem[],
                totalCount: 0,
                page: 1,
                pageSize: 30,
                totalPages: 0,
                catalogEmpty: false
              })
            )
          )
        )
      )
      .subscribe((r) => {
        this.catalogModelSuggestions = r.items;
        this.catalogModelEmpty = r.catalogEmpty;
        this.catalogModelSearchLoading = false;
        this.cdr.markForCheck();
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

    this.setupAutoPriceRecalc();
  }

  ngAfterViewInit(): void {
    setTimeout(() => {
      this.setupJobsStickyResizeObserver();
      this.scheduleJobsStickyMeasure();
    }, 0);
  }

  private setupJobsStickyResizeObserver(): void {
    const wrap = this.jobsTableWrapRef?.nativeElement;
    if (!wrap || this.jobsStickyResizeObserver) {
      return;
    }
    this.jobsStickyResizeObserver = new ResizeObserver(() => this.scheduleJobsStickyMeasure());
    this.jobsStickyResizeObserver.observe(wrap);
  }

  ngOnDestroy(): void {
    clearTimeout(this.searchDebounceTimer);
    this.jobsStickyResizeObserver?.disconnect();
    cancelAnimationFrame(this.jobsStickyMeasureRaf);
    this.pickupDestinationDistanceSub?.unsubscribe();
    this.catalogMakeSearchSub?.unsubscribe();
    this.catalogModelSearchSub?.unsubscribe();
    this.createJobClientFetchSub?.unsubscribe();
    this.accountRatesSub?.unsubscribe();
    this.priceRecalcSub?.unsubscribe();
    this.priceRecalcRequested$.complete();
    this.loadJobsRequestSub?.unsubscribe();
    this.loadJobsRequest$.complete();
  }

  setJobsTableStickySide(index: number, side: 'left' | 'right'): void {
    const cur = this.jobsTableStickyMode[index];
    if (cur === side) {
      this.jobsTableStickyMode[index] = 'off';
    } else {
      this.jobsTableStickyMode[index] = side;
    }
    this.saveJobsTableStickyConfig();
    this.rebuildStickyStyleCache();
    this.scheduleJobsStickyMeasure();
  }

  getStickyThStyle(colIndex: number): Record<string, string> {
    // Returns a cached reference; updated only by `rebuildStickyStyleCache`.
    return this.cachedThStyles[colIndex] ?? {};
  }

  getStickyTdStyle(colIndex: number): Record<string, string> {
    return this.cachedTdStyles[colIndex] ?? {};
  }

  private buildStickyCellStyle(colIndex: number, isHeader: boolean): Record<string, string> {
    const zBase = isHeader ? 30 : 20;
    const mode = this.jobsTableStickyMode[colIndex];
    if (mode === 'off') {
      return {};
    }
    if (mode === 'left') {
      const left = this.stickyLeftPx[colIndex];
      if (left === undefined) {
        return {};
      }
      return {
        position: 'sticky',
        left: `${left}px`,
        zIndex: String(zBase + colIndex),
        boxShadow: '2px 0 6px -2px rgba(0, 0, 0, 0.08)'
      };
    }
    const right = this.stickyRightPx[colIndex];
    if (right === undefined) {
      return {};
    }
    return {
      position: 'sticky',
      right: `${right}px`,
      zIndex: String(zBase + 25 + (JOBS_TABLE_COL_COUNT - 1 - colIndex)),
      boxShadow: '-2px 0 6px -2px rgba(0, 0, 0, 0.08)'
    };
  }

  private rebuildStickyStyleCache(): void {
    const ths = new Array<Record<string, string>>(JOBS_TABLE_COL_COUNT);
    const tds = new Array<Record<string, string>>(JOBS_TABLE_COL_COUNT);
    for (let i = 0; i < JOBS_TABLE_COL_COUNT; i++) {
      ths[i] = this.buildStickyCellStyle(i, true);
      tds[i] = this.buildStickyCellStyle(i, false);
    }
    this.cachedThStyles = ths;
    this.cachedTdStyles = tds;
  }

  private scheduleJobsStickyMeasure(): void {
    cancelAnimationFrame(this.jobsStickyMeasureRaf);
    this.jobsStickyMeasureRaf = requestAnimationFrame(() => {
      this.measureJobsStickyOffsets();
      this.jobsStickyMeasureRaf = requestAnimationFrame(() => this.measureJobsStickyOffsets());
    });
  }

  private measureJobsStickyOffsets(): void {
    const row = this.jobsTableHeaderRowRef?.nativeElement;
    if (!row) {
      return;
    }
    const cells = row.querySelectorAll('th');
    if (cells.length !== JOBS_TABLE_COL_COUNT) {
      return;
    }

    const nextLeft: (number | undefined)[] = new Array(JOBS_TABLE_COL_COUNT).fill(undefined);
    let left = 0;
    for (let i = 0; i < JOBS_TABLE_COL_COUNT; i++) {
      if (this.jobsTableStickyMode[i] === 'left') {
        nextLeft[i] = left;
        left += cells[i].getBoundingClientRect().width;
      }
    }
    this.stickyLeftPx = nextLeft;

    const nextRight: (number | undefined)[] = new Array(JOBS_TABLE_COL_COUNT).fill(undefined);
    let right = 0;
    for (let i = JOBS_TABLE_COL_COUNT - 1; i >= 0; i--) {
      if (this.jobsTableStickyMode[i] === 'right') {
        nextRight[i] = right;
        right += cells[i].getBoundingClientRect().width;
      }
    }
    this.stickyRightPx = nextRight;
    this.rebuildStickyStyleCache();
    this.cdr.detectChanges();
  }

  private loadJobsTableStickyConfig(): void {
    const raw = localStorage.getItem(JOBS_STICKY_STORAGE_KEY);
    if (!raw) {
      this.jobsTableStickyMode = defaultJobsTableStickyConfig();
      return;
    }
    try {
      const parsed = JSON.parse(raw) as unknown;
      if (!Array.isArray(parsed) || parsed.length !== JOBS_TABLE_COL_COUNT) {
        this.jobsTableStickyMode = defaultJobsTableStickyConfig();
        return;
      }
      if (typeof parsed[0] === 'boolean') {
        this.jobsTableStickyMode = migrateJobsTableStickyFromBooleans(parsed as boolean[]);
        this.saveJobsTableStickyConfig();
        return;
      }
      const modes: JobTableStickyPin[] = [];
      for (let i = 0; i < JOBS_TABLE_COL_COUNT; i++) {
        const v = parsed[i];
        if (v === 'off' || v === 'left' || v === 'right') {
          modes.push(v);
        } else {
          modes.push('off');
        }
      }
      this.jobsTableStickyMode = modes;
    } catch {
      this.jobsTableStickyMode = defaultJobsTableStickyConfig();
    }
  }

  private saveJobsTableStickyConfig(): void {
    try {
      localStorage.setItem(JOBS_STICKY_STORAGE_KEY, JSON.stringify(this.jobsTableStickyMode));
    } catch {
      // ignore quota / private mode
    }
  }

  private resetCatalogPickers(): void {
    this.catalogMakeSearchDraft = '';
    this.catalogModelSearchDraft = '';
    this.catalogMakeSuggestions = [];
    this.catalogModelSuggestions = [];
    this.catalogEmpty = false;
    this.catalogModelEmpty = false;
    this.showMakeDropdown = false;
    this.showModelDropdown = false;
    this.lastPickedCatalogMakeName = null;
    this.createJobForm.patchValue(
      { vehicle: { vehicleCatalogMakeId: null } },
      { emitEvent: false }
    );
  }

  /** Model field enabled once make is committed or typed (catalog is optional autocomplete). */
  modelFieldEnabled(): boolean {
    if (this.useExistingVehicle) {
      return false;
    }
    const committed = (this.createJobForm.get('vehicle.vehicleMake')?.value ?? '').toString().trim();
    const draft = (this.catalogMakeSearchDraft ?? '').trim();
    return committed.length > 0 || draft.length > 0;
  }

  /**
   * Commits make draft → `vehicleMake` (SSOT). Drops catalog link if text no longer matches a catalog pick.
   * Clears model when make text changes.
   */
  private commitVehicleMakeFromDraft(): void {
    if (this.useExistingVehicle) {
      return;
    }
    const t = (this.catalogMakeSearchDraft ?? '').trim();
    const prevCommitted = (this.createJobForm.get('vehicle.vehicleMake')?.value ?? '').trim();

    const picked = this.lastPickedCatalogMakeName?.trim() ?? '';
    const matchesPick =
      picked.length > 0 &&
      t.length > 0 &&
      t.localeCompare(picked, undefined, { sensitivity: 'accent' }) === 0;

    const currentId = this.createJobForm.get('vehicle.vehicleCatalogMakeId')?.value as number | null | undefined;
    let nextCatalogId: number | null = null;
    if (matchesPick && currentId != null && typeof currentId === 'number') {
      nextCatalogId = currentId;
    }
    if (!matchesPick) {
      this.lastPickedCatalogMakeName = null;
    }

    if (prevCommitted !== t) {
      this.createJobForm.patchValue({ vehicle: { vehicleModel: '' } }, { emitEvent: false });
      this.catalogModelSearchDraft = '';
      this.catalogModelSuggestions = [];
    }

    this.createJobForm.patchValue(
      {
        vehicle: {
          vehicleMake: t,
          vehicleCatalogMakeId: nextCatalogId
        }
      },
      { emitEvent: true }
    );
  }

  /** Commits model draft → `vehicleModel` (SSOT). */
  private commitModelFromDraft(): void {
    if (this.useExistingVehicle) {
      return;
    }
    const t = (this.catalogModelSearchDraft ?? '').trim();
    this.createJobForm.patchValue({ vehicle: { vehicleModel: t } }, { emitEvent: true });
  }

  onMakeSearchInput(value: string): void {
    if (this.useExistingVehicle) {
      return;
    }
    this.catalogMakeSearchDraft = value;
    this.catalogMakeSearchLoading = true;
    this.showMakeDropdown = true;
    this.makeSearch$.next(value.trim());
  }

  onMakeSearchFocus(): void {
    if (this.useExistingVehicle) {
      return;
    }
    this.showMakeDropdown = true;
    if (!this.catalogMakeSearchDraft.trim() && this.catalogMakeSuggestions.length === 0) {
      this.catalogMakeSearchLoading = true;
      this.makeSearch$.next('');
    }
  }

  onMakeSearchBlur(): void {
    this.commitVehicleMakeFromDraft();
    setTimeout(() => {
      this.showMakeDropdown = false;
      this.cdr.markForCheck();
    }, 200);
  }

  selectCatalogMake(item: VehicleCatalogMakeItem): void {
    this.lastPickedCatalogMakeName = item.name;
    this.createJobForm.patchValue({
      vehicle: {
        vehicleMake: item.name,
        vehicleCatalogMakeId: item.id
      }
    });
    this.catalogMakeSearchDraft = item.name;
    this.showMakeDropdown = false;
    this.createJobForm.get('vehicle.vehicleModel')?.patchValue('');
    this.catalogModelSearchDraft = '';
    this.catalogModelSuggestions = [];
    this.triggerModelSearch(item.id, '');
  }

  onModelSearchInput(value: string): void {
    if (this.useExistingVehicle) {
      return;
    }
    this.catalogModelSearchDraft = value;
    const makeId = this.createJobForm.get('vehicle.vehicleCatalogMakeId')?.value;
    if (makeId == null || typeof makeId !== 'number') {
      this.showModelDropdown = false;
      this.catalogModelSearchLoading = false;
      return;
    }
    this.catalogModelSearchLoading = true;
    this.showModelDropdown = true;
    this.modelSearch$.next({ makeId, q: value.trim() });
  }

  onModelSearchFocus(): void {
    if (this.useExistingVehicle) {
      return;
    }
    if (!this.modelFieldEnabled()) {
      return;
    }
    const makeId = this.createJobForm.get('vehicle.vehicleCatalogMakeId')?.value;
    if (makeId == null || typeof makeId !== 'number') {
      return;
    }
    this.showModelDropdown = true;
    if (!this.catalogModelSearchDraft.trim() && this.catalogModelSuggestions.length === 0) {
      this.catalogModelSearchLoading = true;
      this.modelSearch$.next({ makeId, q: '' });
    }
  }

  onModelSearchBlur(): void {
    this.commitModelFromDraft();
    setTimeout(() => {
      this.showModelDropdown = false;
      this.cdr.markForCheck();
    }, 200);
  }

  selectCatalogModel(item: VehicleCatalogModelItem): void {
    this.createJobForm.patchValue({ vehicle: { vehicleModel: item.name } });
    this.catalogModelSearchDraft = item.name;
    this.showModelDropdown = false;
  }

  private triggerModelSearch(makeId: number, q: string): void {
    this.catalogModelSearchLoading = true;
    this.modelSearch$.next({ makeId, q });
  }

  private loadDispatchOfficeForDisplay(force = false): void {
    if (this.dispatchOfficeLoaded && !force) {
      return;
    }
    this.dispatchOfficeLoaded = true;
    this.locationService.getOfficeLocation().subscribe({
      next: (o) => {
        this.dispatchOfficeCoords = { lat: o.lat, lng: o.lng };
        this.dispatchOfficeLoadError = null;
        this.cdr.markForCheck();
      },
      error: () => {
        this.dispatchOfficeCoords = null;
        this.dispatchOfficeLoadError = 'Could not load dispatch office location.';
        this.dispatchOfficeLoaded = false; // allow retry on next modal open
        this.cdr.markForCheck();
      }
    });
  }

  /** Loads system settings (default hookup fee) lazily — only needed when the
   *  create-job pricing flow runs. */
  private ensureSystemSettingsLoaded(): void {
    if (this.settingsLoaded) {
      return;
    }
    this.settingsLoaded = true;
    this.settingsService.getSettings().subscribe({
      next: (s) => {
        this.defaultHookupFromSettings = Number(s.defaultPricingHookupFee ?? 0) || 0;
        this.applySelectedServicePricing();
        this.cdr.markForCheck();
      },
      error: () => {
        this.defaultHookupFromSettings = 0;
        this.settingsLoaded = false; // allow retry
        this.applySelectedServicePricing();
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
    this.loadJobsRequest$.next();
  }

  /**
   * Replaces a single row in the current `jobs` page with the updated DTO
   * returned by a mutation endpoint. Avoids the full table re-fetch that
   * `loadJobs()` previously triggered after every status/assign/billing
   * change while still keeping the user's view in sync.
   */
  private patchJobInList(updated: Job | null | undefined): void {
    if (!updated || updated.id === undefined || updated.id === null) {
      return;
    }
    const idx = this.jobs.findIndex((j) => j.id === updated.id);
    if (idx === -1) {
      return;
    }
    const next = this.jobs.slice();
    next[idx] = { ...this.jobs[idx], ...updated };
    this.jobs = next;
    this.cdr.markForCheck();
  }

  loadVehicles(force = false): void {
    if (this.vehiclesLoaded && !force) {
      return;
    }
    this.vehicleService.getAllVehicles()
      .pipe(
        catchError(error => {
          console.error('Failed to load vehicles:', error);
          return of([]);
        })
      )
      .subscribe(vehicles => {
        this.vehicles = vehicles;
        this.vehiclesLoaded = true;
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

  private fetchClientsForCreateJobQuery(query: string): Observable<User[]> {
    let params = new HttpParams().set('pageNumber', '1').set('pageSize', '100').set('isActive', 'true');
    const trimmed = query.trim();
    if (trimmed) {
      params = params.set('search', trimmed);
    }
    return this.apiService.get<PagedResponse<User>>('users/clients', params).pipe(
      map((r) => r.data ?? []),
      catchError((error) => {
        console.error('Failed to load clients:', error);
        return of([] as User[]);
      })
    );
  }

  private runCreateJobClientFetch(query: string): Observable<User[]> {
    this.createJobClientSearchLoading = true;
    this.cdr.markForCheck();
    return this.fetchClientsForCreateJobQuery(query).pipe(
      finalize(() => {
        this.createJobClientSearchLoading = false;
        this.cdr.markForCheck();
      })
    );
  }

  /** Active fleet only — create job API rejects inactive truck ids. */
  get trucksForCreateJob(): TruckListItem[] {
    return this.trucks.filter((t) => t.isActive);
  }

  loadTrucks(force = false): void {
    if (this.trucksLoaded && !force) {
      return;
    }
    this.trucksLoading = true;
    this.truckService
      .getAll(true)
      .pipe(
        finalize(() => {
          this.trucksLoading = false;
          this.cdr.markForCheck();
        }),
        catchError((error) => {
          console.error('Failed to load trucks:', error);
          return of([] as TruckListItem[]);
        })
      )
      .subscribe((rows) => {
        this.trucks = rows;
        this.trucksLoaded = true;
      });
  }

  private parseOptionalTruckId(raw: unknown): number | undefined {
    if (raw === null || raw === undefined || raw === '') {
      return undefined;
    }
    const n = Number(raw);
    return Number.isFinite(n) ? n : undefined;
  }

  loadDrivers(force = false): void {
    if (this.driversLoaded && !force) {
      return;
    }
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
        this.driversLoaded = true;
      });
  }

  onStatusFilterChange(): void {
    this.pageNumber = 1;
    this.loadJobs();
  }

  onSearchChange(): void {
    clearTimeout(this.searchDebounceTimer);
    this.searchDebounceTimer = setTimeout(() => {
      this.pageNumber = 1;
      this.loadJobs();
    }, 400);
  }

  /** Immediate search (Enter key) without waiting for debounce. */
  submitJobSearchNow(): void {
    clearTimeout(this.searchDebounceTimer);
    this.pageNumber = 1;
    this.loadJobs();
  }

  onPageSizeChange(): void {
    this.pageNumber = 1;
    this.loadJobs();
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.pageNumber = page;
      this.loadJobs();
    }
  }

  nextPage(): void {
    if (this.hasNextPage) {
      this.pageNumber++;
      this.loadJobs();
    }
  }

  previousPage(): void {
    if (this.hasPreviousPage) {
      this.pageNumber--;
      this.loadJobs();
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

  clearJobFilters(): void {
    this.statusFilter = '';
    this.searchTerm = '';
    this.pageNumber = 1;
    this.loadJobs();
  }

  hasActiveJobFilters(): boolean {
    return !!(this.statusFilter || this.searchTerm.trim());
  }

  /** Initial search / create modal from dashboard quick actions (`?search=`, `?create=1`). */
  private applyJobsRouteQueryParams(): void {
    const map = this.route.snapshot.queryParamMap;
    const search = map.get('search');
    if (search != null && search !== '') {
      this.searchTerm = search;
    }
    const create = map.get('create');
    if (create === '1' || create === 'true') {
      setTimeout(() => {
        this.openCreateJobModal();
        this.cdr.markForCheck();
      }, 0);
    }
  }

  openCreateJobModal(): void {
    this.showQuoteReviewModal = false;
    this.showCreateJobModal = true;
    this.activeCreateJobTab = 'details';
    this.useExistingClient = true;
    this.useExistingVehicle = true;
    this.invoiceServiceItems = [];
    this.createJobValidationAttempted = false;
    this.createJobForm.reset();
    this.createJobAccountServiceRates = [];
    this.resetCreateJobClientSelectUi();
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
    // Lazy data loads — these are no-ops on subsequent opens (idempotent guards).
    this.loadVehicles();
    this.loadDrivers();
    this.loadTrucks();
    this.loadServicePricingProfiles();
    this.loadDispatchOfficeForDisplay();
    this.ensureSystemSettingsLoaded();
    this.loadInsuranceAccountsForJob();
    this.syncVehicleOptionsForCurrentClient();
    this.resetCatalogPickers();
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
        this.applySelectedServicePricing();
      });
  }

  loadServicePricingProfiles(force = false): void {
    if (this.servicePricingProfilesLoaded && !force) {
      return;
    }
    this.servicePricingProfilesLoading = true;
    this.servicePricingService
      .getAll(false)
      .pipe(
        catchError(() => of([] as ServicePricingProfile[])),
        finalize(() => (this.servicePricingProfilesLoading = false))
      )
      .subscribe((rows) => {
        this.servicePricingProfilesLoaded = true;
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

  /**
   * Matches server `PricingCalculatorService`: account×service row → service catalog → legacy account BC + hookup.
   * Base (fixed) appears as a service line item; per-mile rates prefilled on invoice charge fields when catalog defines them.
   */
  private applySelectedServicePricing(): void {
    const serviceTypeRaw = this.createJobForm.get('serviceType')?.value;
    const selectedServiceType = String(serviceTypeRaw || '').trim();
    const charges = this.createJobForm.get('invoiceCharges');
    if (!charges) {
      return;
    }

    const baseSuffix = ' (base)';
    this.invoiceServiceItems = this.invoiceServiceItems.filter(
      (item: any) => !String(item.serviceName || '').endsWith(baseSuffix)
    );

    const accountRaw = this.createJobForm.get('account')?.value;
    const accountId =
      accountRaw !== null && accountRaw !== undefined && String(accountRaw).trim() !== ''
        ? Number(accountRaw)
        : NaN;
    const account =
      Number.isFinite(accountId) && accountId > 0
        ? this.insuranceAccounts.find((a) => a.id === accountId)
        : undefined;

    if (!selectedServiceType && !account) {
      this.calculateInvoiceTotals();
      return;
    }

    const profile = this.getSelectedServicePricingProfile();
    const matrixRow =
      profile && account
        ? this.createJobAccountServiceRates.find((r) => r.servicePricingProfileId === profile.id)
        : undefined;

    let basePrice = 0;
    let rateEnroute = 0;
    let rateLoaded = 0;
    let rateDeadhead = 0;
    let hookEnabled = false;
    let hookAmount = 0;
    let baseLabel = '';

    if (matrixRow) {
      basePrice = Number(matrixRow.basePrice) || 0;
      rateLoaded = Number(matrixRow.loadedPricePerMile ?? matrixRow.pricePerMile) || 0;
      rateEnroute =
        Number(matrixRow.enroutePricePerMile ?? profile?.enroutePricePerMile ?? null) || 0;
      rateDeadhead =
        Number(matrixRow.deadheadPricePerMile ?? profile?.deadheadPricePerMile ?? null) || 0;
      hookEnabled = false;
      hookAmount = 0;
      baseLabel = (matrixRow.serviceName || profile?.name || selectedServiceType).trim();
    } else if (profile) {
      basePrice = Number(profile.basePrice) || 0;
      rateLoaded = Number(profile.loadedPricePerMile ?? profile.pricePerMile) || 0;
      rateEnroute = Number(profile.enroutePricePerMile ?? null) || 0;
      rateDeadhead = Number(profile.deadheadPricePerMile ?? null) || 0;
      hookEnabled = false;
      hookAmount = 0;
      baseLabel = profile.name;
    } else if (account) {
      basePrice = 0;
      rateEnroute = 0;
      rateLoaded = Number(account.rateBC) || 0;
      rateDeadhead = 0;
      hookAmount = this.defaultHookupFromSettings;
      hookEnabled = hookAmount > 0;
      baseLabel = (selectedServiceType || account.name || 'Account').trim();
    }

    if (basePrice > 0 && baseLabel) {
      this.invoiceServiceItems.unshift({
        serviceName: `${baseLabel}${baseSuffix}`,
        quantity: 1,
        price: basePrice,
        total: basePrice
      });
    }

    charges.patchValue({
      unloadedEnrouteMileagePrice: rateEnroute,
      deadHeadMileagePrice: rateDeadhead,
      loadedHookedMileagePrice: rateLoaded,
      hookupFee: hookEnabled ? hookAmount : 0
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

  openDispatcherCashCallRates(): void {
    const accountRaw = this.createJobForm.get('account')?.value;
    const accountId =
      accountRaw !== null && accountRaw !== undefined && String(accountRaw).trim() !== ''
        ? Number(accountRaw)
        : NaN;
    if (!Number.isFinite(accountId) || accountId <= 0) {
      return;
    }

    const account = this.insuranceAccounts.find((a) => a.id === accountId);
    void this.router.navigate(['/dispatcher/accounts', accountId, 'cash-call'], {
      queryParams: account?.name ? { accountName: account.name } : undefined
    });
  }

  formatCreateJobClientLabel(client: User): string {
    const phoneOrEmail = client.phoneNumber || client.email || 'N/A';
    return `${client.fullName} - ${phoneOrEmail}`;
  }

  openCreateJobClientSelect(): void {
    this.createJobClientSelectOpen = true;
    this.createJobClientInstant$.next(this.createJobClientSearch);
  }

  onCreateJobClientSearchChange(): void {
    this.createJobClientSelectOpen = true;
    const id = this.createJobForm.get('client.clientId')?.value?.toString().trim();
    if (id) {
      const cached = this.createJobSelectedClient;
      const label =
        cached && cached.id === id ? this.formatCreateJobClientLabel(cached) : '';
      if (!label || this.createJobClientSearch.trim() !== label.trim()) {
        this.createJobForm.get('client.clientId')?.patchValue('');
        this.createJobSelectedClient = null;
      }
    }
    this.createJobClientDebounced$.next(this.createJobClientSearch);
  }

  selectCreateJobClient(client: User, event?: Event): void {
    event?.preventDefault();
    this.createJobSelectedClient = client;
    this.createJobForm.get('client.clientId')?.patchValue(client.id);
    this.createJobClientSearch = this.formatCreateJobClientLabel(client);
    this.createJobClientSelectOpen = false;
  }

  private resetCreateJobClientSelectUi(): void {
    this.createJobClientSearch = '';
    this.createJobClientSelectOpen = false;
    this.createJobClientSearchResults = [];
    this.createJobSelectedClient = null;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClickCloseCreateJobClientSelect(event: MouseEvent): void {
    if (!this.createJobClientSelectOpen || !this.showCreateJobModal) {
      return;
    }
    const root = this.createJobClientSelectRoot?.nativeElement;
    if (root?.contains(event.target as Node)) {
      return;
    }
    this.createJobClientSelectOpen = false;
  }
  
  onClientModeChange(): void {
    this.resetCreateJobClientSelectUi();
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
          vehicleCatalogMakeId: null,
          vehicleModel: '',
          vehicleYear: '',
          vehicleColor: ''
        });
        this.resetCatalogPickers();
      } else {
        vehicleGroup.patchValue({ vehicleId: '' });
        this.resetCatalogPickers();
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
    this.showQuoteReviewModal = false;
    this.showCreateJobModal = false;
    this.activeCreateJobTab = 'details';
    this.createJobForm.reset();
    this.resetCreateJobClientSelectUi();
    this.resetCatalogPickers();
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
    this.requestPriceRecalc();
  }

  updateServiceItemTotal(index: number): void {
    const item = this.invoiceServiceItems[index];
    if (item) {
      item.total = item.quantity * item.price;
      this.calculateInvoiceTotals();
      this.requestPriceRecalc();
    }
  }

  async calculatePrice(): Promise<void> {
    // Stale-response guard: if the user keeps editing while a quote is in
    // flight, only the newest call is allowed to mutate the form.
    const myGen = ++this.priceRecalcGeneration;
    const isStale = (): boolean => myGen !== this.priceRecalcGeneration;

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
        if (isStale()) return;
        const officeRef = `${office.lat},${office.lng}`;
        const [ab, bc, ca] = await Promise.all([
          this.computeDrivingMiles(officeRef, pickup),
          this.computeDrivingMiles(pickup, destination),
          this.computeDrivingMiles(destination, officeRef)
        ]);
        if (isStale()) return;

        this.officeToPickupMiles = ab;
        this.pickupToDestinationMiles = bc;
        this.dropoffToOfficeMiles = ca;

        milesAB = ab;
        milesBC = bc;
        milesCA = ca;

        // emitEvent: false → do not re-trigger the auto-recalc pipeline.
        charges.patchValue(
          {
            unloadedEnrouteMileageQuantity: milesAB,
            loadedHookedMileageQuantity: milesBC,
            deadHeadMileageQuantity: milesCA
          },
          { emitEvent: false }
        );
      }

      const serviceItemsTotal = this.invoiceServiceItems.reduce((sum: number, item: any) => sum + (item.quantity * item.price), 0);
      const svcProfile = this.getSelectedServicePricingProfile();
      const serviceTypeStr = String(this.createJobForm.get('serviceType')?.value || '').trim();
      const quote = await firstValueFrom(this.pricingService.quote({
        accountId: Number.isFinite(accountId as number) ? accountId : undefined,
        accountName: accountName || undefined,
        servicePricingProfileId: svcProfile?.id,
        serviceName: serviceTypeStr || undefined,
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
      if (isStale()) return;

      this.latestQuote = quote;
      // emitEvent: false → keeps the auto-recalc pipeline from looping on our own writes.
      charges.patchValue(
        {
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
        },
        { emitEvent: false }
      );
      this.createJobForm.patchValue({ cost: quote.grandTotal }, { emitEvent: false });
      if (this.showQuoteReviewModal) {
        this.quoteReviewSummary = this.buildQuoteReviewSummary();
        this.cdr.markForCheck();
        this.scheduleQuotePdfPreviewRefresh();
      }
    } catch (error: any) {
      if (isStale()) return;
      this.createJobError = error?.error?.message || 'Failed to calculate pricing. Please review account, addresses, and pricing values.';
    } finally {
      if (!isStale()) {
        this.calculatingPrice = false;
      }
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

  onViewQuote(): void {
    if (!this.useExistingVehicle) {
      this.commitVehicleMakeFromDraft();
      this.commitModelFromDraft();
    }
    this.createJobValidationAttempted = true;
    this.markAllFieldsAsTouched();
    const validationErrors = this.getValidationErrors();
    if (validationErrors.length > 0) {
      this.validationErrorList = validationErrors;
      this.createJobError = `Please fix ${validationErrors.length} error${validationErrors.length > 1 ? 's' : ''} below`;
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
    this.quoteSmsPhone = this.getDefaultQuoteSmsPhone();
    this.quoteReviewSummary = this.buildQuoteReviewSummary();
    this.showQuoteReviewModal = true;
    this.scheduleQuotePdfPreviewRefresh();
  }

  /** After `quoteReviewSummary` / hidden template updates, capture DOM then build PDF for iframe. */
  private scheduleQuotePdfPreviewRefresh(): void {
    if (this.quoteReviewHtmlPreviewOnly) {
      return;
    }
    setTimeout(() => {
      void this.refreshQuotePdfPreview();
    }, 0);
  }

  closeQuoteReviewModal(): void {
    this.showQuoteReviewModal = false;
    this.quoteReviewSummary = null;
    this.revokeQuotePdfObjectUrl();
    this.quotePdfGenerating = false;
    this.quotePdfError = null;
    if (!this.quoteEmailSending) {
      this.showQuoteEmailModal = false;
      this.quoteEmailError = null;
      this.quoteEmailSuccess = null;
    }
  }

  /**
   * Client-side "Save as PDF": clones the rendered <app-quote-print> sheet into
   * a body-level print host, isolates it via global `body.printing-quote` rules
   * (see styles.scss), and triggers the browser's native print dialog. The
   * user picks "Save as PDF" — output is vector + selectable text and uses the
   * exact same DOM/CSS the modal preview shows. Original quote was server-side
   * via Playwright; that path was retired because the IIS host kept failing on
   * Chromium driver dispatch and we now own the layout end-to-end in Angular.
   */
  async printQuoteSheet(): Promise<void> {
    const summary = this.quoteReviewSummary;
    const sheetEl = this.quotePrintRef?.sheet;
    if (!summary || !sheetEl) {
      return;
    }

    this.quotePdfDownloading = true;
    this.cdr.markForCheck();

    let host: HTMLDivElement | null = null;
    let cleanedUp = false;
    const cleanup = () => {
      if (cleanedUp) return;
      cleanedUp = true;
      document.body.classList.remove('printing-quote');
      if (host && host.parentNode) {
        host.parentNode.removeChild(host);
      }
      window.removeEventListener('afterprint', cleanup);
      this.quotePdfDownloading = false;
      this.cdr.markForCheck();
    };

    try {
      await this.prepareQuoteSheetForCanvasCapture(sheetEl);

      host = document.createElement('div');
      host.id = 'qs-print-host';
      host.appendChild(sheetEl.cloneNode(true));
      const safeRef = summary.quoteRef.replace(/[^\w.-]+/g, '_');
      const previousTitle = document.title;
      document.title = `quote-${safeRef}`;
      document.body.appendChild(host);
      document.body.classList.add('printing-quote');

      window.addEventListener('afterprint', cleanup);

      await new Promise<void>((resolve) =>
        requestAnimationFrame(() => requestAnimationFrame(() => resolve()))
      );

      window.print();
      document.title = previousTitle;

      window.setTimeout(cleanup, 1000);
    } catch {
      cleanup();
    }
  }

  private revokeQuotePdfObjectUrl(): void {
    if (this.quotePdfBlobUrl) {
      URL.revokeObjectURL(this.quotePdfBlobUrl);
      this.quotePdfBlobUrl = null;
    }
    this.quotePdfSafeUrl = null;
  }

  /** Build PDF from HTML quote (Font Awesome + layout) and show in modal iframe; fallback to vector PDF if capture fails. */
  async refreshQuotePdfPreview(): Promise<void> {
    const summary = this.quoteReviewSummary;
    if (!summary) {
      return;
    }
    this.revokeQuotePdfObjectUrl();
    this.quotePdfGenerating = true;
    this.quotePdfError = null;
    this.cdr.markForCheck();
    try {
      let blob: Blob;
      try {
        blob = await this.renderQuotePdfBlobFromHtmlSheet();
      } catch {
        const doc = await this.renderQuotePdfDocument(summary);
        blob = doc.output('blob');
      }
      this.quotePdfBlobUrl = URL.createObjectURL(blob);
      this.quotePdfSafeUrl = this.sanitizer.bypassSecurityTrustResourceUrl(this.quotePdfBlobUrl);
    } catch {
      this.quotePdfError = 'Could not generate PDF preview. Try Download as PDF or refresh the page.';
    } finally {
      this.quotePdfGenerating = false;
      this.cdr.markForCheck();
    }
  }

  /**
   * Ensures webfonts (e.g. Font Awesome) and every `img` inside the quote sheet are ready before
   * html2canvas runs — avoids empty payment logos and mis-sized icons when the sheet is off-screen.
   */
  private async prepareQuoteSheetForCanvasCapture(sheetEl: HTMLElement): Promise<void> {
    await document.fonts?.ready?.catch(() => undefined);
    const imgs = [...sheetEl.querySelectorAll<HTMLImageElement>('img')];
    await Promise.all(imgs.map((img) => this.waitForQuoteSheetImage(img)));
    await new Promise<void>((r) => requestAnimationFrame(() => requestAnimationFrame(() => r())));
  }

  private async waitForQuoteSheetImage(img: HTMLImageElement): Promise<void> {
    const src = (img.currentSrc || img.getAttribute('src') || '').trim();
    if (!src) {
      return;
    }
    if (!img.complete) {
      await new Promise<void>((resolve) => {
        img.addEventListener('load', () => resolve(), { once: true });
        img.addEventListener('error', () => resolve(), { once: true });
      });
    }
    if (img.naturalWidth === 0) {
      return;
    }
    try {
      await img.decode();
    } catch {
      /* decode() can reject for some SVG / cross-origin edge cases */
    }
  }

  /** Rasterize `QuotePrintComponent` DOM (same source as download). */
  private async renderQuotePdfBlobFromHtmlSheet(): Promise<Blob> {
    const sheetEl = this.quotePrintRef?.sheet;
    if (!sheetEl) {
      throw new Error('Quote sheet not ready');
    }
    await this.prepareQuoteSheetForCanvasCapture(sheetEl);
    const html2canvas = (await import('html2canvas')).default;
    const { jsPDF } = await import('jspdf');
    const canvas = await html2canvas(sheetEl, {
      scale: 2,
      useCORS: true,
      logging: false,
      backgroundColor: '#ffffff',
      imageTimeout: 20000
    });
    const imgData = canvas.toDataURL('image/jpeg', 0.96);
    const pdfW = 612;
    const pdfH = Math.ceil((canvas.height / canvas.width) * pdfW);
    const doc = new jsPDF({ unit: 'pt', format: [pdfW, pdfH] });
    doc.addImage(imgData, 'JPEG', 0, 0, pdfW, pdfH);
    return doc.output('blob');
  }

  buildQuoteReviewSummary(): QuoteReviewSummary {
    const raw: any = this.createJobForm.getRawValue();
    const mileage: { label: string; value: string }[] = [];

    if (
      this.officeToPickupMiles != null ||
      this.pickupToDestinationMiles != null ||
      this.dropoffToOfficeMiles != null
    ) {
      mileage.push(
        {
          label: 'Enroute',
          value: this.officeToPickupMiles != null ? `${this.officeToPickupMiles} mi` : '—'
        },
        {
          label: 'Loaded',
          value: this.pickupToDestinationMiles != null ? `${this.pickupToDestinationMiles} mi` : '—'
        },
        {
          label: 'Deadhead',
          value: this.dropoffToOfficeMiles != null ? `${this.dropoffToOfficeMiles} mi` : '—'
        }
      );
    } else {
      const ch = raw.invoiceCharges || {};
      mileage.push(
        { label: 'Enroute (mi)', value: String(ch.unloadedEnrouteMileageQuantity ?? '0') },
        { label: 'Loaded (mi)', value: String(ch.loadedHookedMileageQuantity ?? '0') },
        { label: 'Deadhead (mi)', value: String(ch.deadHeadMileageQuantity ?? '0') }
      );
    }

    const client: { label: string; value: string }[] = [
      { label: 'Client', value: this.formatQuoteClientSummaryLine() }
    ];

    const pickup = String(raw.pickupLocation ?? '').trim() || '—';
    const destination = String(raw.destinationAddress ?? raw.dropoffLocation ?? '').trim() || '—';
    const serviceType = String(raw.serviceType ?? '').trim() || '—';

    let vehicleLabel = '—';
    if (this.useExistingVehicle && raw.vehicle?.vehicleId) {
      const vid = parseInt(String(raw.vehicle.vehicleId), 10);
      const veh =
        this.vehicles.find((x) => x.id === vid) || this.filteredVehicles.find((x) => x.id === vid);
      vehicleLabel = veh ? this.getVehicleDisplay(veh) : `Vehicle ID ${raw.vehicle.vehicleId}`;
    } else {
      const y = raw.vehicle?.vehicleYear;
      const mk = raw.vehicle?.vehicleMake;
      const md = raw.vehicle?.vehicleModel;
      const col = raw.vehicle?.vehicleColor;
      const plate = raw.vehicle?.licensePlate;
      const st = raw.vehicle?.licenseState;
      const desc = [y, mk, md]
        .filter((x) => x != null && String(x).trim() !== '')
        .map((x) => String(x).trim())
        .join(' ');
      if (desc) {
        vehicleLabel = col ? `${desc} (${col})` : desc;
      } else {
        const vin = String(raw.vehicle?.vehicleVin ?? '').trim();
        vehicleLabel = vin ? `VIN ${vin}` : '—';
      }
      if (plate) {
        const pl = `${plate}${st ? ' (' + st + ')' : ''}`;
        vehicleLabel = vehicleLabel === '—' ? `Plate ${pl}` : `${vehicleLabel} · ${pl}`;
      }
    }

    const q = this.latestQuote;
    const totalAmount = q ? this.formatCurrency(q.grandTotal) : this.formatCurrency(this.getGrandTotal());
    const now = new Date();
    const validUntil = new Date(now);
    validUntil.setDate(validUntil.getDate() + 7);
    const dateFmt: Intl.DateTimeFormatOptions = { year: 'numeric', month: 'long', day: 'numeric' };
    const quoteDateDisplay = now.toLocaleDateString('en-US', dateFmt);
    const validUntilDisplay = validUntil.toLocaleDateString('en-US', dateFmt);
    const y = now.getFullYear();
    const quoteRef = `QT-${y}-${String(Math.floor(Math.random() * 1_000_000)).padStart(6, '0')}`;

    const fromLines: string[] = [];
    const co = String(raw.companyName ?? '').trim();
    if (co) {
      fromLines.push(co);
    }
    const dispatchPhone = String(raw.contactPhoneNumber ?? '').trim();
    if (dispatchPhone) {
      fromLines.push(dispatchPhone);
    }

    const toLines: string[] = [];
    const cn = String(raw.contactName ?? '').trim();
    if (cn) {
      toLines.push(cn);
    }
    const clientLine = this.formatQuoteClientSummaryLine();
    if (clientLine && clientLine !== '—') {
      toLines.push(clientLine);
    }
    if (toLines.length === 0) {
      toLines.push('—');
    }

    const lineItems = this.buildQuoteLineItems(serviceType, raw);

    let totalsSubtotal: string;
    let totalsTaxLabel: string;
    let totalsTax: string;
    if (q) {
      totalsSubtotal = this.formatCurrency(q.taxableAmount);
      totalsTaxLabel = q.taxExempt ? 'Tax (exempt)' : `Tax (${q.taxPercent}%)`;
      totalsTax = this.formatCurrency(q.taxAmount);
    } else {
      totalsSubtotal = this.formatCurrency(this.getTaxableAmount());
      const taxExempt = !!raw.invoiceCharges?.taxExempt;
      const taxPct = parseFloat(raw.invoiceCharges?.taxPercent ?? '0') || 0;
      totalsTaxLabel = taxExempt ? 'Tax (exempt)' : `Tax (${taxPct}%)`;
      totalsTax = this.formatCurrency(this.getTaxes());
    }

    const notesRaw = String(raw.notes ?? '').trim();
    const notesPreview =
      notesRaw ||
      'Additional information about the job can be added in the job notes before sending this quote.';

    // Company info (from form or defaults)
    const companyName = co || 'Strong Towing';
    const companyAddress = String(raw.companyAddress ?? '5652 Columbia Pike, Falls Church, VA 22041, USA').trim();
    const companyPhone = dispatchPhone || String(raw.companyPhone ?? '').trim();
    const companyEmail = String(raw.companyEmail ?? 'dispatch@strongtowing.services').trim();
    const companyWebsite = String(raw.companyWebsite ?? 'strongtowing.services').trim();

    // Client name / phone split
    const clientRawLine = this.formatQuoteClientSummaryLine();
    const clientPhoneMatch = clientRawLine.match(/\(([^)]+)\)$/);
    const clientName = clientPhoneMatch
      ? clientRawLine.slice(0, clientRawLine.lastIndexOf('(')).trim()
      : clientRawLine;
    const clientPhone = clientPhoneMatch ? clientPhoneMatch[1] : '';

    // Service date (today)
    const serviceDateDisplay = now.toLocaleDateString('en-US', dateFmt);

    // Truck type label
    const truckIdVal = raw.truckId;
    let truckTypeLabel = '—';
    if (truckIdVal) {
      const selectedTruck = this.trucks.find((t) => t.id === Number(truckIdVal));
      if (selectedTruck) {
        truckTypeLabel = selectedTruck.truckTypeName || selectedTruck.unitLabel || '—';
      }
    }

    // Loaded mileage for location section
    const loadedMile = mileage.find((m) => m.label === 'Loaded' || m.label === 'Loaded (mi)');
    let loadedMileageDisplay = loadedMile ? loadedMile.value : '—';
    if (loadedMileageDisplay && loadedMileageDisplay !== '—') {
      const original = loadedMileageDisplay;
      const cleaned = original.replace(/,/g, '').replace(/\s*mi\b/gi, '').trim();
      const n = parseFloat(cleaned);
      const looksNumeric =
        cleaned.length > 0 &&
        Number.isFinite(n) &&
        /^-?\d+(\.\d+)?$/.test(cleaned);
      loadedMileageDisplay = looksNumeric
        ? `${n.toFixed(n % 1 === 0 ? 0 : 2)} MI`
        : original;
    }

    // Tax percent string
    const taxPercent = q
      ? q.taxExempt ? '0' : String(q.taxPercent ?? '0')
      : String(parseFloat(raw.invoiceCharges?.taxPercent ?? '0') || 0);

    return {
      quoteRef,
      quoteDateDisplay,
      validUntilDisplay,
      fromLines,
      toLines,
      lineItems,
      totalsSubtotal,
      totalsTaxLabel,
      totalsTax,
      mileage,
      client,
      pickup,
      destination,
      serviceType,
      vehicleLabel,
      totalAmount,
      notesPreview,
      footerNote: `Generated ${now.toLocaleString()}`,
      companyName,
      companyAddress,
      companyPhone,
      companyEmail,
      companyWebsite,
      clientName,
      clientPhone,
      clientDisplayLine: clientRawLine && clientRawLine !== '—' ? clientRawLine : 'Not specified',
      serviceDateDisplay,
      truckTypeLabel,
      loadedMileageDisplay,
      taxPercent
    };
  }

  /**
   * Fixed six-row breakdown to match the quote PDF design. Extra invoice line items are bucketed by name;
   * server `extraItemsTotal` fills gaps into Other Charges.
   */
  private buildQuoteLineItems(serviceType: string, raw: any): QuoteLineItem[] {
    const q = this.latestQuote;
    const ch = raw.invoiceCharges || {};
    const fmt = (n: number) => this.formatCurrency(n);

    type Agg = { qty: number; unitPrice: number; amount: number };
    const emptyAgg = (): Agg => ({ qty: 0, unitPrice: 0, amount: 0 });

    const pushAgg = (target: Agg, qty: number, price: number): void => {
      const line = qty * price;
      target.amount += line;
      target.qty += qty;
      if (qty > 0 && price > 0) {
        target.unitPrice = price;
      }
    };

    const winch = emptyAgg();
    const fuel = emptyAgg();
    const afterHours = emptyAgg();
    const other = emptyAgg();

    for (const item of this.invoiceServiceItems) {
      const name = String(item.serviceName || '').toLowerCase();
      const qty = Number(item.quantity) || 0;
      const price = Number(item.price) || 0;
      if (/winch|labor/.test(name)) {
        pushAgg(winch, qty, price);
      } else if (/fuel|environmental/.test(name)) {
        pushAgg(fuel, qty, price);
      } else if (/after\s*hours?|afterhours/.test(name)) {
        pushAgg(afterHours, qty, price);
      } else {
        pushAgg(other, qty, price);
      }
    }

    if (q && q.extraItemsTotal > 0) {
      const allocated = winch.amount + fuel.amount + afterHours.amount + other.amount;
      const gap = q.extraItemsTotal - allocated;
      if (gap > 0.005) {
        other.amount += gap;
        if (other.qty < 1) {
          other.qty = 1;
        }
        other.unitPrice = gap;
      }
    }

    let hookup = q ? q.hookupFee : parseFloat(ch.hookupFee) || 0;
    let loadedQty: number;
    let loadedRate: number;
    let loadedAmt: number;
    if (q) {
      loadedQty = q.billableMiles;
      loadedRate = q.rateBC;
      loadedAmt = q.chargeBC;
    } else {
      const fullQty = parseFloat(ch.loadedHookedMileageQuantity) || 0;
      loadedRate = parseFloat(ch.loadedHookedMileagePrice) || 0;
      const freeAllow = this.latestQuote?.pricingFreeMilesAllowance ?? 0;
      loadedQty = Math.max(0, fullQty - freeAllow);
      loadedAmt = loadedQty * loadedRate;
    }

    const extrasRow = (description: string, subtitle: string, agg: Agg): QuoteLineItem => {
      const qtyStr = agg.qty > 0 ? String(agg.qty) : '0';
      let unitStr = fmt(0);
      if (agg.qty > 0 && agg.amount > 0) {
        unitStr = fmt(agg.amount / agg.qty);
      } else if (agg.unitPrice > 0) {
        unitStr = fmt(agg.unitPrice);
      }
      return {
        description,
        subtitle,
        quantity: qtyStr,
        unitPrice: unitStr,
        amount: fmt(agg.amount)
      };
    };

    const hookRow: QuoteLineItem = {
      description: 'Hookup Fee',
      subtitle: 'Covers truck dispatch, setup and hookup.',
      quantity: '1',
      unitPrice: fmt(hookup),
      amount: fmt(hookup)
    };

    const loadedRow: QuoteLineItem = {
      description: 'Loaded Mileage',
      subtitle: 'Charged per loaded mile from pickup to destination.',
      quantity: loadedQty > 0 ? `${loadedQty} mi` : '0 mi',
      unitPrice: loadedRate > 0 ? `${fmt(loadedRate)} / mile` : fmt(0),
      amount: fmt(loadedAmt)
    };

    return [
      hookRow,
      loadedRow,
      extrasRow('Winch / Labor (if needed)', 'Additional labor or winching.', winch),
      extrasRow('Fuel / Environmental Fee', 'Fuel surcharge and environmental compliance.', fuel),
      extrasRow('After Hours Fee', 'Applies for service outside standard business hours.', afterHours),
      extrasRow('Other Charges', 'Miscellaneous charges as applicable.', other)
    ];
  }

  /** Client as "Name (phone)" for quote modal, PDF, and SMS body (no separate contact fields). */
  formatQuoteClientSummaryLine(): string {
    const raw: any = this.createJobForm.getRawValue();
    if (this.useExistingClient && raw.client?.clientId) {
      const cid = String(raw.client.clientId);
      const cli =
        this.createJobSelectedClient && String(this.createJobSelectedClient.id) === cid
          ? this.createJobSelectedClient
          : null;
      let clientLine = `Client ID ${cid}`;
      if (cli) {
        const name = String(cli.fullName ?? '').trim();
        const phone = String(cli.phoneNumber ?? '').trim();
        if (name && phone) {
          clientLine = `${name} (${phone})`;
        } else if (name) {
          clientLine = name;
        } else if (phone) {
          clientLine = phone;
        } else {
          clientLine = this.formatCreateJobClientLabel(cli);
        }
      }
      return clientLine;
    }
    const name = String(raw.client?.clientFullName ?? '').trim();
    const phone = String(raw.client?.clientPhoneNumber ?? '').trim();
    if (name && phone) {
      return `${name} (${phone})`;
    }
    if (name) {
      return name;
    }
    if (phone) {
      return phone;
    }
    return '—';
  }

  private getDefaultQuoteSmsPhone(): string {
    const contact = String(this.createJobForm.get('contactPhoneNumber')?.value ?? '').trim();
    if (contact) {
      return contact;
    }
    const newClientPhone = String(this.createJobForm.get('client.clientPhoneNumber')?.value ?? '').trim();
    if (newClientPhone) {
      return newClientPhone;
    }
    const sel = this.createJobSelectedClient?.phoneNumber;
    if (sel && String(sel).trim()) {
      return String(sel).trim();
    }
    return '';
  }

  buildQuoteDocumentText(options?: { forSms?: boolean }): string {
    const raw: any = this.createJobForm.getRawValue();
    const lines: string[] = [];
    const push = (s: string) => lines.push(s);
    const pushKv = (label: string, value: string | number | null | undefined) => {
      const t = value === null || value === undefined ? '' : String(value).trim();
      push(`${label}: ${t || '—'}`);
    };

    push('STRONG TOWING — SERVICE QUOTE (ESTIMATE)');
    if (this.quoteReviewSummary?.quoteRef) {
      push(`Reference: ${this.quoteReviewSummary.quoteRef}`);
    }
    push('This quote is an estimate and does not create a job in the system.');
    push('');
    pushKv('Company', raw.companyName);
    const accRaw = raw.account;
    if (accRaw !== null && accRaw !== undefined && String(accRaw).trim() !== '') {
      const id = Number(accRaw);
      const acc = this.insuranceAccounts.find((a) => a.id === id);
      pushKv('Account', acc ? this.insuranceAccountOptionLabel(acc) : String(accRaw));
    } else {
      pushKv('Account', '');
    }
    if (raw.companyOverride) {
      pushKv('Company override (invoice)', raw.companyOverride);
    }
    pushKv('Call type', raw.callType);
    pushKv('Priority', raw.priority);
    pushKv('Service type', raw.serviceType);
    const svcProf = this.getSelectedServicePricingProfile();
    if (svcProf) {
      pushKv('Pricing profile', svcProf.name);
    }
    push('');
    push('--- Client ---');
    pushKv('Client', this.formatQuoteClientSummaryLine());
    push('');
    push('--- Locations ---');
    pushKv('Pickup', raw.pickupLocation);
    pushKv('Destination', raw.destinationAddress || raw.dropoffLocation);
    if (
      this.officeToPickupMiles != null ||
      this.pickupToDestinationMiles != null ||
      this.dropoffToOfficeMiles != null
    ) {
      push('');
      push('--- Driving distances (mi) ---');
      pushKv('Enroute', this.officeToPickupMiles != null ? this.officeToPickupMiles : '—');
      pushKv('Loaded', this.pickupToDestinationMiles != null ? this.pickupToDestinationMiles : '—');
      pushKv('Deadhead', this.dropoffToOfficeMiles != null ? this.dropoffToOfficeMiles : '—');
    }
    push('');
    push('--- Vehicle ---');
    if (this.useExistingVehicle && raw.vehicle?.vehicleId) {
      const vid = parseInt(String(raw.vehicle.vehicleId), 10);
      const veh =
        this.vehicles.find((x) => x.id === vid) || this.filteredVehicles.find((x) => x.id === vid);
      pushKv('Vehicle', veh ? this.getVehicleDisplay(veh) : `Vehicle ID ${raw.vehicle.vehicleId}`);
    } else {
      pushKv('VIN', raw.vehicle?.vehicleVin);
      pushKv('Make', raw.vehicle?.vehicleMake);
      pushKv('Model', raw.vehicle?.vehicleModel);
      pushKv('Year', raw.vehicle?.vehicleYear);
      pushKv('Color', raw.vehicle?.vehicleColor);
      const plate = raw.vehicle?.licensePlate;
      const st = raw.vehicle?.licenseState;
      pushKv('License plate', plate ? `${plate}${st ? ' (' + st + ')' : ''}` : '');
    }
    if (raw.notes) {
      push('');
      push('--- Notes ---');
      push(String(raw.notes));
    }
    if (raw.billingNotes) {
      push('');
      push('--- Billing notes ---');
      push(String(raw.billingNotes));
    }

    const ch = raw.invoiceCharges || {};
    push('');
    push('--- Invoice charge inputs ---');
    pushKv('Hookup fee', this.formatCurrency(parseFloat(ch.hookupFee) || 0));
    pushKv('Unloaded en-route (mi × $/mi)', `${ch.unloadedEnrouteMileageQuantity ?? 0} × ${this.formatCurrency(parseFloat(ch.unloadedEnrouteMileagePrice) || 0)}`);
    pushKv('Loaded / hooked (mi × $/mi)', `${ch.loadedHookedMileageQuantity ?? 0} × ${this.formatCurrency(parseFloat(ch.loadedHookedMileagePrice) || 0)}`);
    pushKv('Deadhead (mi × $/mi)', `${ch.deadHeadMileageQuantity ?? 0} × ${this.formatCurrency(parseFloat(ch.deadHeadMileagePrice) || 0)}`);
    pushKv('Discount ($)', this.formatCurrency(parseFloat(ch.discount) || 0));
    pushKv('Discount (%)', String(ch.discountPercent ?? 0));
    pushKv('Service charge (%)', String(ch.serviceChargePercent ?? 0));
    pushKv('Tax (%)', String(ch.taxPercent ?? 0));
    pushKv('Tax exempt', ch.taxExempt ? 'Yes' : 'No');
    if (ch.manualTotalOverride != null && ch.manualTotalOverride !== '') {
      pushKv('Manual total override', String(ch.manualTotalOverride));
      pushKv('Override reason', ch.manualOverrideReason);
    }

    if (this.invoiceServiceItems.length > 0) {
      push('');
      push('--- Extra service line items ---');
      for (const item of this.invoiceServiceItems) {
        const name = String(item.serviceName || '').trim() || '(line item)';
        const qty = Number(item.quantity) || 0;
        const price = Number(item.price) || 0;
        push(`  • ${name}  qty ${qty} @ ${this.formatCurrency(price)} = ${this.formatCurrency(qty * price)}`);
      }
    }

    const q = this.latestQuote;
    if (q) {
      push('');
      push('--- Server quote (Calculate Price) ---');
      pushKv('Account (quote)', q.accountName ?? '');
      pushKv('Miles A→B / B→C / C→A', `${q.milesAB} / ${q.milesBC} / ${q.milesCA}`);
      pushKv('Billable miles (loaded)', String(q.billableMiles));
      pushKv('Free miles applied', String(q.freeMilesApplied));
      pushKv('Pricing free-mile allowance', String(q.pricingFreeMilesAllowance));
      pushKv('Hookup fee', this.formatCurrency(q.hookupFee));
      pushKv('Rate A→B / B→C / C→A per mi', `${this.formatCurrency(q.rateAB)} / ${this.formatCurrency(q.rateBC)} / ${this.formatCurrency(q.rateCA)}`);
      pushKv('Charge A→B / B→C / C→A', `${this.formatCurrency(q.chargeAB)} / ${this.formatCurrency(q.chargeBC)} / ${this.formatCurrency(q.chargeCA)}`);
      pushKv('Extra items total', this.formatCurrency(q.extraItemsTotal));
      pushKv('Base subtotal', this.formatCurrency(q.baseSubtotal));
      pushKv('Discount', this.formatCurrency(q.discountAmount));
      pushKv('After discount', this.formatCurrency(q.afterDiscount));
      pushKv('Service charge', `${q.serviceChargePercent}% → ${this.formatCurrency(q.serviceChargeAmount)}`);
      pushKv('Taxable amount', this.formatCurrency(q.taxableAmount));
      pushKv(`Tax (${q.taxExempt ? 'exempt' : String(q.taxPercent) + '%'})`, this.formatCurrency(q.taxAmount));
      pushKv('Grand total (server)', this.formatCurrency(q.grandTotal));
      if (q.manualTotalOverrideApplied) {
        pushKv('Manual override applied', String(q.manualTotalOverrideApplied));
        if (q.manualTotalOverride != null) {
          pushKv('Manual override total', this.formatCurrency(Number(q.manualTotalOverride)));
        }
        if (q.manualOverrideReason) {
          pushKv('Manual override reason', q.manualOverrideReason);
        }
      }
    } else if (!options?.forSms) {
      push('');
      push('Tip: Use "Calculate Price" on the Payment tab for a detailed server-side quote breakdown.');
    }

    push('');
    push('--- Totals (from form / calculator) ---');
    pushKv('Subtotal', this.formatCurrency(this.getSubtotal()));
    pushKv('Taxes', this.formatCurrency(this.getTaxes()));
    pushKv('Grand total', this.formatCurrency(this.getGrandTotal()));
    pushKv('Final cost field', this.formatCurrency(parseFloat(raw.cost) || 0));

    push('');
    push(`Generated ${new Date().toLocaleString()}`);

    return lines.join('\n');
  }

  /** Maps `QuoteReviewSummary` → `QuotePrintData` expected by `QuotePrintComponent`. */
  buildQuotePrintData(summary: QuoteReviewSummary): QuotePrintData {
    return {
      quoteRef: summary.quoteRef,
      quoteDateDisplay: summary.quoteDateDisplay,
      validUntilDisplay: summary.validUntilDisplay,
      companyName: summary.companyName,
      companyAddress: summary.companyAddress,
      companyPhone: summary.companyPhone,
      companyEmail: summary.companyEmail,
      companyWebsite: summary.companyWebsite,
      clientName: summary.clientName,
      clientPhone: summary.clientPhone,
      clientDisplayLine: summary.clientDisplayLine,
      serviceType: summary.serviceType,
      serviceDateDisplay: summary.serviceDateDisplay,
      truckTypeLabel: summary.truckTypeLabel,
      pickup: summary.pickup,
      destination: summary.destination,
      loadedMileageDisplay: summary.loadedMileageDisplay,
      lineItems: summary.lineItems,
      totalsSubtotal: summary.totalsSubtotal,
      totalsTaxLabel: summary.totalsTaxLabel,
      totalsTax: summary.totalsTax,
      taxPercent: summary.taxPercent,
      totalAmount: summary.totalAmount,
      notesPreview: summary.notesPreview
    };
  }

  private async renderQuotePdfDocument(summary: QuoteReviewSummary): Promise<import('jspdf').jsPDF> {
    const { jsPDF } = await import('jspdf');
    const doc = new jsPDF({ unit: 'pt', format: 'letter' });
    const margin = 48;
    let y = margin;
    const pageW = doc.internal.pageSize.getWidth();
    const pageH = doc.internal.pageSize.getHeight();
    const contentW = pageW - margin * 2;
    const lineH = 11.5;
    const gap = 6;
    const primary: [number, number, number] = [44, 62, 80];
    const lightGray: [number, number, number] = [242, 242, 242];
    const mid = margin + contentW / 2 + 6;
    const colW = contentW / 2 - 20;
    const totW = 220;
    const totX = pageW - margin - totW;
    const cQty = pageW - margin - 168;
    const cUnit = pageW - margin - 108;
    const cAmt = pageW - margin - 8;

    const ensureSpace = (h: number) => {
      if (y + h > pageH - margin) {
        doc.addPage();
        y = margin;
      }
    };

    const hr = () => {
      ensureSpace(gap + 4);
      y += 4;
      doc.setDrawColor(220, 220, 220);
      doc.line(margin, y, pageW - margin, y);
      y += gap + 6;
    };

    const writeParagraph = (text: string, x: number, maxW: number, fontSize = 9) => {
      doc.setFont('helvetica', 'normal');
      doc.setFontSize(fontSize);
      doc.setTextColor(...primary);
      const lines = doc.splitTextToSize(text, maxW);
      for (const line of lines) {
        ensureSpace(lineH);
        doc.text(line, x, y);
        y += lineH;
      }
    };

    const columnHeight = (lines: string[], x: number, startY: number, w: number): number => {
      let yy = startY;
      doc.setFont('helvetica', 'normal');
      doc.setFontSize(9);
      doc.setTextColor(...primary);
      for (const ln of lines) {
        const parts = doc.splitTextToSize(ln, w);
        for (const p of parts) {
          doc.text(p, x, yy);
          yy += lineH;
        }
      }
      return yy;
    };

    // Header — left: logo (from logo.svg embedded PNG) or company name; right: QUOTE + meta
    const yHeader = y;
    const logoDataUrl = await this.getQuoteLogoPngDataUrl();
    const logoWPt = 108;
    const logoHPt = (32 / 142) * logoWPt;
    if (logoDataUrl) {
      try {
        doc.addImage(logoDataUrl, 'PNG', margin, yHeader, logoWPt, logoHPt);
      } catch {
        doc.setTextColor(...primary);
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(12);
        doc.text(summary.fromLines[0] || 'Strong Towing', margin, yHeader + 12);
      }
    } else {
      doc.setTextColor(...primary);
      doc.setFont('helvetica', 'bold');
      doc.setFontSize(12);
      doc.text(summary.fromLines[0] || 'Strong Towing', margin, yHeader + 12);
    }
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(8);
    doc.setTextColor(120, 128, 136);
    const taglineY = logoDataUrl ? yHeader + logoHPt + 8 : yHeader + 26;
    doc.text('Service quote (estimate)', margin, taglineY);
    doc.setTextColor(...primary);

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(20);
    doc.text('QUOTE', pageW - margin, yHeader + 14, { align: 'right' });
    doc.setFontSize(10);
    doc.text(summary.quoteRef, pageW - margin, yHeader + 30, { align: 'right' });
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(9);
    doc.text(`Date: ${summary.quoteDateDisplay}`, pageW - margin, yHeader + 44, { align: 'right' });
    doc.text(`Valid until: ${summary.validUntilDisplay}`, pageW - margin, yHeader + 56, { align: 'right' });

    y = yHeader + 72;
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(8);
    doc.setTextColor(100, 110, 120);
    writeParagraph('This document is a price estimate only and does not book a job until submitted as a regular call.', margin, contentW, 8);
    doc.setTextColor(...primary);
    y += 4;
    hr();

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(9);
    doc.text('FROM', margin, y);
    doc.text('TO', mid, y);
    y += lineH + 2;
    const yFromStart = y;
    const endFrom = columnHeight(summary.fromLines.slice(1), margin, yFromStart, colW);
    const endTo = columnHeight(summary.toLines, mid, yFromStart, colW);
    y = Math.max(endFrom, endTo) + gap;
    hr();

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(9);
    doc.text('SERVICE & VEHICLE', margin, y);
    doc.text('LOCATIONS', mid, y);
    y += lineH + 2;
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(9);
    ensureSpace(lineH * 4);
    doc.text(`Service type: ${summary.serviceType}`, margin, y);
    doc.text(`Vehicle: ${summary.vehicleLabel}`, margin, y + lineH);
    const pickupLines = doc.splitTextToSize(`Pickup: ${summary.pickup}`, colW);
    let yR = y;
    for (const pl of pickupLines) {
      doc.text(pl, mid, yR);
      yR += lineH;
    }
    const destLines = doc.splitTextToSize(`Destination: ${summary.destination}`, colW);
    for (const dl of destLines) {
      doc.text(dl, mid, yR);
      yR += lineH;
    }
    y = Math.max(y + lineH * 2, yR) + gap;
    hr();

    if (summary.mileage.length > 0) {
      doc.setFont('helvetica', 'bold');
      doc.setFontSize(9);
      doc.text('Driving distances', margin, y);
      y += lineH + 2;
      doc.setFont('helvetica', 'normal');
      for (const m of summary.mileage) {
        ensureSpace(lineH);
        doc.text(`${m.label}: ${m.value}`, margin, y);
        y += lineH;
      }
      y += gap;
      hr();
    }

    // Line items table
    ensureSpace(28);
    doc.setFillColor(...lightGray);
    doc.rect(margin, y - 2, contentW, 16, 'F');
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(8);
    doc.setTextColor(...primary);
    doc.text('DESCRIPTION', margin + 4, y + 9);
    doc.text('QTY', cQty, y + 9);
    doc.text('UNIT PRICE', cUnit, y + 9, { align: 'right' });
    doc.text('AMOUNT', cAmt, y + 9, { align: 'right' });
    y += 22;
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(9);
    for (const row of summary.lineItems) {
      const descW = cQty - margin - 14;
      const descLines = doc.splitTextToSize(row.description, descW);
      const h = Math.max(lineH, descLines.length * lineH);
      ensureSpace(h + 6);
      doc.text(descLines, margin + 4, y);
      doc.text(row.quantity, cQty, y);
      doc.text(row.unitPrice, cUnit, y, { align: 'right' });
      doc.text(row.amount, cAmt, y, { align: 'right' });
      y += h + 4;
    }

    y += 10;
    // Totals
    ensureSpace(72);
    doc.setFillColor(...lightGray);
    doc.rect(totX, y - 6, totW, 18, 'F');
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(9);
    doc.setTextColor(...primary);
    doc.text('Subtotal', totX + 8, y + 6);
    doc.text(summary.totalsSubtotal, pageW - margin - 8, y + 6, { align: 'right' });
    y += 22;
    doc.setFillColor(255, 255, 255);
    doc.rect(totX, y - 6, totW, 18, 'F');
    doc.setTextColor(...primary);
    doc.text(summary.totalsTaxLabel, totX + 8, y + 6);
    doc.text(summary.totalsTax, pageW - margin - 8, y + 6, { align: 'right' });
    y += 22;
    doc.setFillColor(...primary);
    doc.rect(totX, y - 6, totW, 22, 'F');
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(255, 255, 255);
    doc.text('Total', totX + 8, y + 8);
    doc.text(summary.totalAmount, pageW - margin - 8, y + 8, { align: 'right' });
    doc.setTextColor(0, 0, 0);
    doc.setFont('helvetica', 'normal');
    y += 32;

    hr();
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(9);
    doc.setTextColor(...primary);
    doc.text('NOTES', margin, y);
    doc.text('THANK YOU', mid, y);
    y += lineH + 2;
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(9);
    const notesH = columnHeight([summary.notesPreview], margin, y, colW);
    const thanksH = columnHeight(['We appreciate your business!'], mid, y, colW);
    y = Math.max(notesH, thanksH) + gap;

    doc.setFontSize(8);
    doc.setTextColor(150, 150, 150);
    writeParagraph(summary.footerNote, margin, contentW, 8);

    return doc;
  }

  /**
   * jsPDF cannot render SVG reliably; `logo.svg` embeds a PNG. Fetch SVG, extract data URL for addImage.
   */
  private async getQuoteLogoPngDataUrl(): Promise<string | null> {
    if (this.quoteLogoPngDataUrl !== undefined) {
      return this.quoteLogoPngDataUrl;
    }
    try {
      const url = new URL('images/logo.svg', document.baseURI).toString();
      const res = await fetch(url);
      if (!res.ok) {
        this.quoteLogoPngDataUrl = null;
        return null;
      }
      const svg = await res.text();
      const m =
        svg.match(/href="(data:image\/png;base64,[^"]+)"/i) ||
        svg.match(/xlink:href="(data:image\/png;base64,[^"]+)"/i);
      this.quoteLogoPngDataUrl = m?.[1] ?? null;
      return this.quoteLogoPngDataUrl;
    } catch {
      this.quoteLogoPngDataUrl = null;
      return null;
    }
  }

  /**
   * Opens the "Send quote by email" confirmation modal. The recipient defaults from the
   * client email already on the create-job form (or the selected existing client) and is
   * editable; the dispatcher can also tweak the subject and the cover note before
   * confirming. On confirm we POST the structured quote payload to <c>api/quotes/email</c> —
   * the API renders the PDF server-side via Playwright/Chromium and attaches it itself, so
   * Font Awesome glyphs and SVG payment marks stay vector and aligned in the email.
   */
  openEmailWithQuote(): void {
    const summary = this.quoteReviewSummary ?? this.buildQuoteReviewSummary();
    this.quoteReviewSummary = summary;
    this.quoteEmailRecipient = this.getDefaultQuoteEmailRecipient();
    this.quoteEmailSubject = `Quote ${summary.quoteRef} — ${summary.companyName || 'Strong Towing'}`;
    this.quoteEmailMessage = this.getDefaultQuoteEmailMessage(summary);
    this.quoteEmailError = null;
    this.quoteEmailSuccess = null;
    this.quoteEmailSending = false;
    this.showQuoteEmailModal = true;
  }

  closeQuoteEmailModal(): void {
    if (this.quoteEmailSending) {
      return;
    }
    this.showQuoteEmailModal = false;
    this.quoteEmailError = null;
    this.quoteEmailSuccess = null;
  }

  /** Resolves the default recipient: existing client's email when picked, else the form's clientEmail field. */
  private getDefaultQuoteEmailRecipient(): string {
    const raw: any = this.createJobForm.getRawValue();
    if (this.useExistingClient && raw.client?.clientId) {
      const cid = String(raw.client.clientId);
      const cli =
        this.createJobSelectedClient && String(this.createJobSelectedClient.id) === cid
          ? this.createJobSelectedClient
          : null;
      const existingEmail = String(cli?.email ?? '').trim();
      if (existingEmail) {
        return existingEmail;
      }
    }
    return String(raw.client?.clientEmail ?? '').trim();
  }

  private getDefaultQuoteEmailMessage(summary: QuoteReviewSummary): string {
    const greetingName = summary.clientName?.trim();
    const lines: string[] = [];
    lines.push(greetingName ? `Hi ${greetingName},` : 'Hi,');
    lines.push('');
    lines.push(
      `Please find your service quote (${summary.quoteRef}) attached. The estimated total is ${summary.totalAmount}.`
    );
    lines.push('');
    lines.push('Reply to this email or give us a call if you have any questions or want to schedule the service.');
    return lines.join('\n');
  }

  /** File name used for the PDF attachment + the hint shown inside the email modal. */
  quoteEmailAttachmentName(): string {
    const ref = (this.quoteReviewSummary?.quoteRef ?? 'quote').replace(/[^\w.-]+/g, '_');
    return `quote-${ref}.pdf`;
  }

  /** Confirm-button enabled when we have a recipient that looks like an email and a quote summary loaded. */
  quoteEmailFormReady(): boolean {
    const to = (this.quoteEmailRecipient ?? '').trim();
    const looksLikeEmail = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(to);
    return looksLikeEmail && !!this.quoteReviewSummary;
  }

  async confirmSendQuoteEmail(): Promise<void> {
    if (!this.quoteEmailFormReady() || this.quoteEmailSending) {
      return;
    }
    const summary = this.quoteReviewSummary;
    if (!summary) {
      return;
    }

    this.quoteEmailSending = true;
    this.quoteEmailError = null;
    this.quoteEmailSuccess = null;
    this.cdr.markForCheck();

    try {
      const print = this.buildQuotePrintData(summary);
      const result = await firstValueFrom(
        this.quoteService.sendQuoteEmail({
          toEmail: this.quoteEmailRecipient.trim(),
          subject: this.quoteEmailSubject?.trim() || null,
          message: this.quoteEmailMessage?.trim() || null,
          quote: print
        })
      );

      if (result?.success) {
        this.quoteEmailSuccess = result.toEmail || this.quoteEmailRecipient.trim();
        this.quoteEmailError = null;
        // Auto-dismiss after a short delay so the dispatcher sees the confirmation.
        setTimeout(() => {
          this.showQuoteEmailModal = false;
          this.quoteEmailSuccess = null;
          this.cdr.markForCheck();
        }, 1800);
      } else {
        this.quoteEmailError = result?.errorMessage || 'Could not send the quote email. Please try again.';
      }
    } catch (err: any) {
      const apiMsg =
        err?.error?.errorMessage ||
        err?.error?.message ||
        err?.message ||
        'Could not send the quote email. Please try again.';
      this.quoteEmailError = apiMsg;
    } finally {
      this.quoteEmailSending = false;
      this.cdr.markForCheck();
    }
  }

  openSmsWithQuote(): void {
    const digits = this.quoteSmsPhone.replace(/\D/g, '');
    if (!digits) {
      return;
    }
    const maxChars = 1600;
    let body = this.buildQuoteDocumentText({ forSms: true });
    if (body.length > maxChars) {
      body = body.slice(0, maxChars - 50) + '\n\n… (truncated — use Print quote for the full layout.)';
    }
    window.location.href = `sms:${digits}?body=${encodeURIComponent(body)}`;
  }

  quoteSmsRecipientReady(): boolean {
    return this.quoteSmsPhone.replace(/\D/g, '').length > 0;
  }

  onCreateJob(): void {
    if (!this.useExistingVehicle) {
      this.commitVehicleMakeFromDraft();
      this.commitModelFromDraft();
    }
    if (this.createJobForm.get('callType')?.value === 'Quote') {
      this.onViewQuote();
      return;
    }
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
    const freeAllowSubmit = this.latestQuote?.pricingFreeMilesAllowance ?? 0;
    const billableLoadedSubmit = Math.max(0, loadedQty - freeAllowSubmit);
    const subtotal = hookupFee + billableLoadedSubmit * loadedPrice + serviceItemsTotal;
    const effectiveDiscount = discount > 0 ? discount : (subtotal * (discountPercent / 100));
    const afterDiscount = Math.max(0, subtotal - effectiveDiscount);
    const serviceChargeAmount = afterDiscount * (serviceChargePercent / 100);
    const taxableAmount = afterDiscount + serviceChargeAmount;
    const taxes = charges?.taxExempt ? 0 : taxableAmount * (taxPercent / 100);
    const grandTotal = taxableAmount + taxes;
    
    const jobData: CreateJobRequest = {
      cost: parseFloat(formValue.cost) || grandTotal,
      serviceType: formValue.serviceType,
      servicePricingProfileId: this.getSelectedServicePricingProfile()?.id,
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
      truckId: this.parseOptionalTruckId(formValue.truckId),
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
          // Mutation endpoint returns the updated Job; patch in place instead of
          // re-fetching the entire page.
          this.patchJobInList(result as unknown as Job);
        }
      });
  }

  /** SuperAdmin / Admin / Dispatcher — assign first driver (Waiting) or reassign (Dispatch…Loaded). */
  canAssignOrReassignDriver(job: Job): boolean {
    return job.status !== JOB_STATUS.Completed && job.status !== JOB_STATUS.Cancelled;
  }

  /** True when the job already had a driver assigned (re-dispatch). */
  get assignDriverModalIsReassign(): boolean {
    return !!this.selectedJob && this.selectedJob.status !== JOB_STATUS.Waiting;
  }

  openAssignDriverModal(job: Job): void {
    if (!this.canAssignOrReassignDriver(job)) {
      this.error = 'Completed or cancelled jobs cannot be assigned or reassigned.';
      return;
    }

    this.selectedJob = job;
    this.showAssignDriverModal = true;
    this.assignDriverForm.patchValue({ driverId: job.driverId || '' });
    // Driver list is only needed once a dispatcher actually opens this dialog.
    this.loadDrivers();
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
          this.patchJobInList(result.job as Job);
        }
      });
  }

  clearPushNotificationWarning(): void {
    this.pushNotificationWarning = null;
  }

  saveJobTruckAssignment(): void {
    if (!this.selectedJob) {
      return;
    }
    if (
      this.selectedJob.status === JOB_STATUS.Completed ||
      this.selectedJob.status === JOB_STATUS.Cancelled
    ) {
      return;
    }
    this.detailTruckSaving = true;
    this.detailTruckError = null;
    this.jobService
      .updateJobTruck(this.selectedJob.id, { truckId: this.detailTruckId })
      .pipe(
        finalize(() => {
          this.detailTruckSaving = false;
        })
      )
      .subscribe({
        next: (updated) => {
          this.selectedJob = updated;
          this.patchJobInList(updated);
        },
        error: (err: { error?: { message?: string } }) => {
          this.detailTruckError = err.error?.message || 'Failed to update truck';
        }
      });
  }

  openJobDetails(job: Job): void {
    this.selectedJob = job;
    this.showJobDetailsModal = true;
    this.detailTruckId = job.truckId ?? null;
    this.detailTruckSaving = false;
    this.detailTruckError = null;
    // Truck list is needed for the in-modal reassignment select.
    this.loadTrucks();
    this.priceOverrideError = null;
    this.priceOverrideSubmitting = false;
    this.priceOverrideReason = '';
    this.priceOverrideCommissionVisible = false;
    this.priceOverrideCost =
      job.cost !== null && job.cost !== undefined && Number.isFinite(Number(job.cost))
        ? Number(job.cost)
        : null;
    this.billingError = null;
    this.billingBillingPaymentMode =
      (job.billingPaymentMode as string) || JOB_BILLING_PAYMENT_MODE.Standard;
    this.billingInsuranceCoveredAmount =
      job.insuranceCoveredAmount !== undefined && job.insuranceCoveredAmount !== null
        ? Number(job.insuranceCoveredAmount)
        : null;
    this.billingClientCoveredAmount =
      job.clientCoveredAmount !== undefined && job.clientCoveredAmount !== null
        ? Number(job.clientCoveredAmount)
        : null;
    this.billingInsurancePortionBilled = !!job.insurancePortionBilled;
    this.billingClientPortionPaid = !!job.clientPortionPaid;
    this.billingDriverCashCollectedAmount =
      job.driverCashCollectedAmount !== undefined && job.driverCashCollectedAmount !== null
        ? Number(job.driverCashCollectedAmount)
        : null;
    this.billingPayrollDeductionAmount =
      job.payrollDeductionAmount !== undefined && job.payrollDeductionAmount !== null
        ? Number(job.payrollDeductionAmount)
        : null;
    this.billingPayrollDeductionRecorded = !!job.payrollDeductionRecorded;
  }

  closeJobDetails(): void {
    this.showJobDetailsModal = false;
    this.selectedJob = null;
    this.detailTruckId = null;
    this.detailTruckError = null;
    this.priceOverrideError = null;
    this.priceOverrideReason = '';
    this.priceOverrideCost = null;
    this.billingError = null;
  }

  canEditBilling(): boolean {
    return this.canCreatePaymentLink();
  }

  saveBillingPayment(): void {
    if (!this.selectedJob || !this.canEditBilling()) {
      return;
    }
    this.billingSubmitting = true;
    this.billingError = null;
    const body: UpdateJobBillingPaymentRequest = {
      billingPaymentMode: this.billingBillingPaymentMode,
      insuranceCoveredAmount: this.billingInsuranceCoveredAmount,
      clientCoveredAmount: this.billingClientCoveredAmount,
      insurancePortionBilled: this.billingInsurancePortionBilled,
      clientPortionPaid: this.billingClientPortionPaid,
      driverCashCollectedAmount: this.billingDriverCashCollectedAmount,
      payrollDeductionAmount: this.billingPayrollDeductionAmount,
      payrollDeductionRecorded: this.billingPayrollDeductionRecorded
    };
    this.jobService.updateJobBillingPayment(this.selectedJob.id, body).subscribe({
      next: (updated) => {
        this.billingSubmitting = false;
        this.selectedJob = updated;
        this.openJobDetails(updated);
        this.patchJobInList(updated);
      },
      error: (err) => {
        this.billingSubmitting = false;
        this.billingError = err.error?.message || err.error?.error || 'Failed to save billing.';
      }
    });
  }

  formatPaymentMethodLabel(method: string | undefined | null): string {
    const m = (method || '').trim();
    const map: Record<string, string> = {
      Card: 'Card',
      PaymentLink: 'Payment link',
      Cash: 'Cash',
      Insurance: 'Insurance (full)',
      CashToDriverPayroll: 'Cash to driver (payroll deduction)',
      SplitInsuranceClient: 'Insurance + client split'
    };
    return map[m] || m || '—';
  }

  /** SuperAdmin and Administrator only — not dispatchers. */
  canOverrideJobPrice(): boolean {
    const u = this.authService.getCurrentUser();
    if (!u) {
      return false;
    }
    const rid = Number(u.roleId);
    return rid === RoleId.SuperAdmin || rid === RoleId.Admin;
  }

  submitPriceOverride(): void {
    if (!this.selectedJob || !this.canOverrideJobPrice()) {
      return;
    }
    const cost = Number(this.priceOverrideCost);
    if (!Number.isFinite(cost) || cost < 0.01 || cost > 999999.99) {
      this.priceOverrideError = 'Enter a valid amount between $0.01 and $999,999.99.';
      return;
    }
    const reason = (this.priceOverrideReason || '').trim();
    if (reason.length < 5) {
      this.priceOverrideError = 'Please enter a reason (at least 5 characters).';
      return;
    }
    const payload: OverrideJobPriceRequest = {
      cost,
      reason,
      commissionVisibleToDriver: this.priceOverrideCommissionVisible
    };
    this.priceOverrideSubmitting = true;
    this.priceOverrideError = null;
    this.jobService.overrideJobPrice(this.selectedJob.id, payload).subscribe({
      next: (updated) => {
        this.selectedJob = updated;
        this.priceOverrideCost =
          updated.cost !== null && updated.cost !== undefined && Number.isFinite(Number(updated.cost))
            ? Number(updated.cost)
            : null;
        this.priceOverrideReason = '';
        this.priceOverrideSubmitting = false;
        this.patchJobInList(updated);
      },
      error: (error: unknown) => {
        this.priceOverrideSubmitting = false;
        const e = error as { error?: { message?: string } };
        this.priceOverrideError = e?.error?.message || 'Failed to update price.';
      }
    });
  }

  getStatusBadgeClass(status: string): string {
    const classes: Record<JobStatus, string> = {
      [JOB_STATUS.Waiting]: 'bg-yellow-100 text-yellow-800',
      [JOB_STATUS.Dispatch]: 'bg-blue-100 text-blue-800',
      [JOB_STATUS.OnRoute]: 'bg-purple-100 text-purple-800',
      [JOB_STATUS.OnScene]: 'bg-indigo-100 text-indigo-800',
      [JOB_STATUS.Loaded]: 'bg-green-100 text-green-800',
      [JOB_STATUS.Completed]: 'bg-gray-100 text-gray-800',
      [JOB_STATUS.Cancelled]: 'bg-red-100 text-red-800'
    };
    return classes[status as JobStatus] || 'bg-gray-100 text-gray-800';
  }

  /** API status string → short label for tables and badges */
  formatJobStatus(status: string): string {
    return formatJobStatusLabel(status);
  }

  /** Styling for each step in the full status strip (job details). */
  jobStatusStepClass(job: Job, step: JobStatus): string {
    const base = 'px-2 py-1 text-xs font-medium rounded-full transition-colors';
    if (job.status === JOB_STATUS.Cancelled) {
      if (step === JOB_STATUS.Cancelled) {
        return `${base} bg-red-100 text-red-800 ring-2 ring-red-300`;
      }
      return `${base} bg-gray-100 text-gray-400 line-through opacity-60`;
    }
    const cur = JOB_STATUS_ORDER.indexOf(job.status);
    const si = JOB_STATUS_ORDER.indexOf(step);
    if (cur < 0 || si < 0) {
      return `${base} bg-gray-100 text-gray-600`;
    }
    if (si < cur) {
      return `${base} bg-emerald-50 text-emerald-800 border border-emerald-200`;
    }
    if (si === cur) {
      return `${base} bg-blue-600 text-white ring-2 ring-blue-500 ring-offset-1`;
    }
    return `${base} bg-gray-50 text-gray-500 border border-gray-200`;
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

  /** Google Maps search URL for an address string (quote modal + PDF links). */
  mapsLinkForAddress(address: string | null | undefined): string | null {
    const t = String(address ?? '').trim();
    if (!t || t === '—') {
      return null;
    }
    return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(t)}`;
  }

  // Helper methods for template calculations
  parseFloat(value: any): number {
    return parseFloat(value) || 0;
  }

  getSubtotal(): number {
    const loadedQty = parseFloat(this.createJobForm.get('invoiceCharges.loadedHookedMileageQuantity')?.value || '0');
    const loadedPrice = parseFloat(this.createJobForm.get('invoiceCharges.loadedHookedMileagePrice')?.value || '0');
    const hookupFee = parseFloat(this.createJobForm.get('invoiceCharges.hookupFee')?.value || '0');
    const serviceItemsTotal = this.invoiceServiceItems.reduce((sum, item) => sum + (item.quantity * item.price), 0);
    const freeAllow = this.latestQuote?.pricingFreeMilesAllowance ?? 0;
    const billableLoaded = Math.max(0, loadedQty - freeAllow);
    // Customer subtotal matches API: loaded (BC) miles only + hook + extras — not enroute/deadhead.
    return hookupFee + billableLoaded * loadedPrice + serviceItemsTotal;
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
    if (s === JOB_STATUS.Completed || s === JOB_STATUS.Cancelled) {
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

