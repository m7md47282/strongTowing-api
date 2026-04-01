import { AfterViewInit, Directive, ElementRef, inject, OnDestroy } from '@angular/core';
import { NgControl } from '@angular/forms';
import { Loader } from '@googlemaps/js-api-loader';
import { environment } from '../../environments/environment';

/**
 * Attaches Google Places Autocomplete to an input. Use with reactive forms: `formControlName` on the same element.
 */
@Directive({
  standalone: true,
  selector: '[appPlacesAutocomplete]',
})
export class PlacesAutocompleteDirective implements AfterViewInit, OnDestroy {
  private readonly el = inject(ElementRef<HTMLInputElement>);
  private readonly ngControl = inject(NgControl, { self: true, optional: true });

  private autocomplete?: google.maps.places.Autocomplete;
  private placeChangedListener?: google.maps.MapsEventListener;

  ngAfterViewInit(): void {
    void this.attach();
  }

  ngOnDestroy(): void {
    if (this.placeChangedListener) {
      google.maps.event.removeListener(this.placeChangedListener);
    }
    this.autocomplete = undefined;
  }

  private async attach(): Promise<void> {
    const apiKey = environment.mapsApiKey?.trim();
    if (!apiKey) {
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
      return;
    }

    const input = this.el.nativeElement;
    this.autocomplete = new google.maps.places.Autocomplete(input, {
      fields: ['formatted_address', 'geometry', 'name', 'address_components'],
    });

    this.placeChangedListener = this.autocomplete.addListener('place_changed', () => {
      const place = this.autocomplete?.getPlace();
      const addr = place?.formatted_address ?? place?.name;
      if (!addr) {
        return;
      }

      if (this.ngControl?.control) {
        this.ngControl.control.setValue(addr, { emitEvent: true });
      } else {
        input.value = addr;
        input.dispatchEvent(new Event('input', { bubbles: true }));
      }
    });
  }
}
