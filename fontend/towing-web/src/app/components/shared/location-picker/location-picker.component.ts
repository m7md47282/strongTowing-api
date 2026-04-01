import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  OnChanges,
  OnDestroy,
  OnInit,
  Output,
  SimpleChanges,
  ViewChild,
} from '@angular/core';
import { Loader } from '@googlemaps/js-api-loader';
import { GoogleMap, MapDirectionsRenderer, MapDirectionsService, MapMarker } from '@angular/google-maps';
import { Subscription } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { LocationService } from '../../../services/location.service';
import type { OfficeLocationDto, RouteResponseDto } from '../../../models/location.model';

export type LocationPickerMode = 'pick' | 'route';

/** Unified Google Maps location UI: `pick` = single marker + map click; `route` = Places search + office/client + distance. */
@Component({
  selector: 'app-location-picker',
  standalone: true,
  imports: [CommonModule, GoogleMap, MapMarker, MapDirectionsRenderer],
  templateUrl: './location-picker.component.html',
  styleUrl: './location-picker.component.scss',
})
export class LocationPickerComponent implements OnInit, OnChanges, AfterViewInit, OnDestroy {
  @Input() mode: LocationPickerMode = 'route';

  /** --- pick mode --- */
  @Input() latitude: number | null = null;
  @Input() longitude: number | null = null;
  @Input() pickHint = 'Search for an address or click the map to set the location.';
  @Input() markerTitle = 'Location';
  @Input() pickMapHeight = '280px';
  @Input() allowMapClick = true;

  /** --- route mode --- */
  @Input() compact = false;

  @Output() positionChange = new EventEmitter<{ lat: number; lng: number }>();
  @Output() routeCalculated = new EventEmitter<{
    distanceInMiles: number;
    durationInMinutes: number;
  }>();

  @ViewChild('addressInput') addressInput?: ElementRef<HTMLInputElement>;
  @ViewChild('pickSearchInput') pickSearchInput?: ElementRef<HTMLInputElement>;

  mapsReady = false;
  mapHeight: string | null = '420px';
  mapWidth: string | null = '100%';

  center: google.maps.LatLngLiteral = { lat: 39.8283, lng: -98.5795 };
  zoom = 4;

  pickMarkerPosition: google.maps.LatLngLiteral = { lat: 39.8283, lng: -98.5795 };

  officePosition: google.maps.LatLngLiteral | null = null;
  clientPosition: google.maps.LatLngLiteral | null = null;

  directions: google.maps.DirectionsResult | null = null;
  readonly directionsRendererOptions: google.maps.DirectionsRendererOptions = {
    suppressMarkers: true,
  };

  calcDistanceMiles: number | null = null;
  durationMinutes: number | null = null;
  loadError: string | null = null;
  routeError: string | null = null;

  private autocomplete?: google.maps.places.Autocomplete;
  private placeChangedListener?: google.maps.MapsEventListener;
  private pickAutocomplete?: google.maps.places.Autocomplete;
  private pickPlaceChangedListener?: google.maps.MapsEventListener;
  private subs = new Subscription();

  constructor(
    private readonly locationService: LocationService,
    private readonly mapDirectionsService: MapDirectionsService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    if (this.mode === 'route' && this.compact) {
      this.mapHeight = '320px';
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (this.mode !== 'pick' || !this.mapsReady) {
      return;
    }
    if (changes['latitude'] || changes['longitude']) {
      this.applyPickInputs();
      this.cdr.markForCheck();
    }
  }

  ngAfterViewInit(): void {
    void this.bootstrap();
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
    if (this.placeChangedListener) {
      google.maps.event.removeListener(this.placeChangedListener);
    }
    if (this.pickPlaceChangedListener) {
      google.maps.event.removeListener(this.pickPlaceChangedListener);
    }
  }

  onPickMapClick(ev: google.maps.MapMouseEvent): void {
    if (this.mode !== 'pick' || !this.allowMapClick) {
      return;
    }
    const ll = ev.latLng;
    if (!ll) {
      return;
    }
    this.updatePickPosition(ll.lat(), ll.lng());
  }

  private async bootstrap(): Promise<void> {
    const apiKey = environment.mapsApiKey?.trim();
    if (!apiKey) {
      this.loadError = 'Maps API key is not configured in environment.';
      this.cdr.markForCheck();
      return;
    }

    const loader = new Loader({
      apiKey,
      version: 'weekly',
      libraries: ['places'],
    });

    try {
      await loader.load();
    } catch {
      this.loadError = 'Could not load Google Maps.';
      this.cdr.markForCheck();
      return;
    }

    if (this.mode === 'pick') {
      this.mapsReady = true;
      this.applyPickInputs();
      this.cdr.markForCheck();
      setTimeout(() => this.initPickAutocomplete(), 0);
      return;
    }

    this.subs.add(
      this.locationService.getOfficeLocation().subscribe({
        next: (office: OfficeLocationDto) => {
          this.officePosition = { lat: office.lat, lng: office.lng };
          this.center = { ...this.officePosition };
          this.zoom = 11;
          this.mapsReady = true;
          this.cdr.markForCheck();
          setTimeout(() => this.initRouteAutocomplete(), 0);
        },
        error: () => {
          this.loadError = 'Could not load office location from server.';
          this.mapsReady = true;
          this.cdr.markForCheck();
          setTimeout(() => this.initRouteAutocomplete(), 0);
        },
      })
    );
  }

  private applyPickInputs(): void {
    const lat = this.latitude;
    const lng = this.longitude;
    if (lat != null && lng != null && !Number.isNaN(lat) && !Number.isNaN(lng)) {
      this.pickMarkerPosition = { lat, lng };
      this.center = { lat, lng };
      this.zoom = 14;
    }
  }

  private updatePickPosition(lat: number, lng: number): void {
    this.pickMarkerPosition = { lat, lng };
    this.center = { lat, lng };
    this.zoom = Math.max(this.zoom, 14);
    this.positionChange.emit({ lat, lng });
    this.cdr.markForCheck();
  }

  private initPickAutocomplete(): void {
    const input = this.pickSearchInput?.nativeElement;
    if (!input || !this.mapsReady || this.mode !== 'pick') {
      return;
    }

    if (this.pickPlaceChangedListener) {
      google.maps.event.removeListener(this.pickPlaceChangedListener);
      this.pickPlaceChangedListener = undefined;
    }

    this.pickAutocomplete = new google.maps.places.Autocomplete(input, {
      fields: ['geometry', 'formatted_address', 'name'],
    });

    this.pickPlaceChangedListener = this.pickAutocomplete.addListener('place_changed', () => {
      const place = this.pickAutocomplete?.getPlace();
      const loc = place?.geometry?.location;
      if (!loc) {
        return;
      }
      this.updatePickPosition(loc.lat(), loc.lng());
    });
  }

  private initRouteAutocomplete(): void {
    const input = this.addressInput?.nativeElement;
    if (!input || !this.mapsReady || this.mode !== 'route') {
      return;
    }

    if (this.placeChangedListener) {
      google.maps.event.removeListener(this.placeChangedListener);
      this.placeChangedListener = undefined;
    }

    this.autocomplete = new google.maps.places.Autocomplete(input, {
      fields: ['geometry', 'formatted_address', 'name'],
      types: ['address'],
    });

    this.placeChangedListener = this.autocomplete.addListener('place_changed', () => {
      const place = this.autocomplete?.getPlace();
      const loc = place?.geometry?.location;
      if (!loc) {
        return;
      }

      const lat = loc.lat();
      const lng = loc.lng();
      this.clientPosition = { lat, lng };
      this.center = { lat, lng };
      this.zoom = 13;
      this.routeError = null;

      this.subs.add(
        this.locationService.calculateDistance(lat, lng).subscribe({
          next: (res: RouteResponseDto) => {
            this.calcDistanceMiles = res.distanceInMiles;
            this.durationMinutes = res.durationInMinutes;
            this.routeCalculated.emit({
              distanceInMiles: res.distanceInMiles,
              durationInMinutes: res.durationInMinutes,
            });
            this.cdr.markForCheck();
          },
          error: () => {
            this.routeError = 'Could not calculate distance. Check API configuration.';
            this.calcDistanceMiles = null;
            this.durationMinutes = null;
            this.cdr.markForCheck();
          },
        })
      );

      if (this.officePosition) {
        this.subs.add(
          this.mapDirectionsService
            .route({
              origin: this.officePosition,
              destination: { lat, lng },
              travelMode: google.maps.TravelMode.DRIVING,
            })
            .subscribe((res) => {
              if (res.status === google.maps.DirectionsStatus.OK && res.result) {
                this.directions = res.result;
              } else {
                this.directions = null;
              }
              this.cdr.markForCheck();
            })
        );
      }

      this.cdr.markForCheck();
    });
  }
}
