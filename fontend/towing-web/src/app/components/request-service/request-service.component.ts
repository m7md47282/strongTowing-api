import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { OrderService } from '../../services/order.service';
import { CreateOrderRequest, CreateOrderResponse } from '../../models/order.model';
import { PaymentService } from '../../services/payment.service';
import { LocationPickerComponent } from '../shared/location-picker/location-picker.component';
import { PlacesAutocompleteDirective } from '../../directives/places-autocomplete.directive';

interface ServiceType {
  id: string;
  name: string;
  description: string;
  icon: string;
  startingPrice: number;
  priceNote: string;
}

interface VehicleType {
  value: string;
  label: string;
}

interface Priority {
  value: string;
  label: string;
  description: string;
}

@Component({
  selector: 'app-request-service',
  imports: [CommonModule, ReactiveFormsModule, RouterModule, LocationPickerComponent, PlacesAutocompleteDirective],
  templateUrl: './request-service.component.html',
  styleUrls: ['./request-service.component.scss']
})
export class RequestServiceComponent implements OnInit {
  serviceRequestForm: FormGroup;
  isSubmitting = false;
  isProcessingPayment = false;
  successMessage = '';
  errorMessage = '';
  selectedService: ServiceType | null = null;
  currentYear = new Date().getFullYear();
  maxYear = this.currentYear + 1;
  lastAuthorizationAmount: number | null = null;

  serviceTypes: ServiceType[] = [
    {
      id: 'towing',
      name: 'Towing Services',
      description: 'Professional towing for all vehicle types with state-of-the-art equipment.',
      icon: 'fas fa-truck',
      startingPrice: 75,
      priceNote: 'First 10 miles included'
    },
    {
      id: 'roadside',
      name: 'Roadside Assistance',
      description: 'Quick and reliable roadside assistance to get you back on the road.',
      icon: 'fas fa-tools',
      startingPrice: 50,
      priceNote: 'Basic service fee'
    },
    {
      id: 'emergency',
      name: 'Emergency Services',
      description: '24/7 emergency towing and roadside assistance when you need it most.',
      icon: 'fas fa-exclamation-triangle',
      startingPrice: 100,
      priceNote: 'Emergency rates apply'
    },
    {
      id: 'long-distance',
      name: 'Long Distance Towing',
      description: 'Reliable long-distance towing services across state lines.',
      icon: 'fas fa-route',
      startingPrice: 200,
      priceNote: 'Per mile pricing'
    },
    {
      id: 'specialty',
      name: 'Specialty Towing',
      description: 'Specialized towing for unique vehicles and situations.',
      icon: 'fas fa-car-crash',
      startingPrice: 150,
      priceNote: 'Specialty rates apply'
    },
    {
      id: 'fleet',
      name: 'Fleet Services',
      description: 'Comprehensive towing and maintenance services for fleet vehicles.',
      icon: 'fas fa-truck-moving',
      startingPrice: 500,
      priceNote: 'Monthly contract rates'
    }
  ];

  vehicleTypes: VehicleType[] = [
    { value: 'car', label: 'Car' },
    { value: 'suv', label: 'SUV' },
    { value: 'truck', label: 'Truck' },
    { value: 'motorcycle', label: 'Motorcycle' },
    { value: 'rv', label: 'RV' },
    { value: 'commercial', label: 'Commercial Vehicle' },
    { value: 'other', label: 'Other' }
  ];

  priorities: Priority[] = [
    { value: 'Low', label: 'Low Priority', description: 'Non-urgent service needed' },
    { value: 'Medium', label: 'Medium Priority', description: 'Service needed within a few hours' },
    { value: 'High', label: 'High Priority', description: 'Urgent service needed' },
    { value: 'Emergency', label: 'Emergency', description: 'Immediate assistance required' }
  ];

  constructor(
    private fb: FormBuilder,
    private orderService: OrderService,
    private paymentService: PaymentService
  ) {
    this.serviceRequestForm = this.fb.group({
      serviceType: ['', Validators.required],
      priority: ['Medium', Validators.required],
      vehicleType: ['', Validators.required],
      vehicleMake: ['', Validators.required],
      vehicleModel: [''],
      vehicleYear: [''],
      vehicleColor: [''],
      licensePlate: [''],
      vin: [''],
      pickupAddress: ['', Validators.required],
      pickupCity: ['', Validators.required],
      pickupState: ['', Validators.required],
      pickupZipCode: ['', Validators.required],
      destinationAddress: [''],
      destinationCity: [''],
      destinationState: [''],
      destinationZipCode: [''],
      description: ['', Validators.required],
      contactName: ['', Validators.required],
      contactPhone: ['', [Validators.required, Validators.pattern(/^[\d\s\-\+\(\)]+$/)]],
      contactEmail: ['', [Validators.email]],
      paymentDueMode: ['PayLater', Validators.required],
      paymentMethod: ['PaymentLink', Validators.required],
      preferredContactTime: [''],
      additionalNotes: ['']
    });
  }

  ngOnInit(): void {
    // Component initialization
  }

  selectService(service: ServiceType): void {
    this.selectedService = service;
    this.serviceRequestForm.patchValue({
      serviceType: service.name,
      description: `Request for ${service.name} service`
    });
  }

  scrollToForm(): void {
    const formElement = document.getElementById('request-form');
    if (formElement) {
      formElement.scrollIntoView({ behavior: 'smooth' });
    }
  }

  onSubmitServiceRequest(): void {
    if (this.serviceRequestForm.valid) {
      this.isSubmitting = true;
      this.errorMessage = '';
      this.successMessage = '';

      const formValue = this.serviceRequestForm.value;
      const estimatedAmount = this.getEstimatedAmount();
      const orderRequest: CreateOrderRequest = {
        serviceType: formValue.serviceType,
        vehicleType: formValue.vehicleType,
        vehicleMake: formValue.vehicleMake,
        vehicleModel: formValue.vehicleModel || '',
        vehicleYear: formValue.vehicleYear ? parseInt(formValue.vehicleYear) : new Date().getFullYear(),
        vehicleColor: formValue.vehicleColor || '',
        licensePlate: formValue.licensePlate || '',
        vin: formValue.vin || '',
        pickupAddress: formValue.pickupAddress,
        pickupCity: formValue.pickupCity,
        pickupState: formValue.pickupState,
        pickupZipCode: formValue.pickupZipCode,
        destinationAddress: formValue.destinationAddress || '',
        destinationCity: formValue.destinationCity || '',
        destinationState: formValue.destinationState || '',
        destinationZipCode: formValue.destinationZipCode || '',
        description: formValue.description,
        priority: formValue.priority as any,
        notes: `Contact phone: ${formValue.contactPhone}${formValue.contactEmail ? `, Email: ${formValue.contactEmail}` : ''}${formValue.preferredContactTime ? `, Preferred contact time: ${formValue.preferredContactTime}` : ''}${formValue.additionalNotes ? `, Additional notes: ${formValue.additionalNotes}` : ''}`,
        contactName: formValue.contactName,
        contactPhone: formValue.contactPhone,
        contactEmail: formValue.contactEmail || undefined,
        amount: estimatedAmount,
        paymentDueMode: formValue.paymentDueMode,
        paymentMethod: formValue.paymentMethod,
        currency: 'usd'
      };

      this.orderService.createOrder(orderRequest).subscribe({
        next: (response) => {
          if (response.fraudStatus === 'UnderReview') {
            this.isSubmitting = false;
            this.successMessage = 'Your request was received and queued for payment review. Our team will contact you shortly.';
            this.serviceRequestForm.reset({
              priority: 'Medium',
              paymentDueMode: 'PayLater',
              paymentMethod: 'PaymentLink'
            });
            this.selectedService = null;
            return;
          }

          if (formValue.paymentDueMode === 'PayNow') {
            this.processPayNowResponse(response);
            return;
          }

          this.isSubmitting = false;
          this.successMessage = 'Service request submitted successfully with payment pending. Our dispatcher will contact you shortly.';
          this.serviceRequestForm.reset({
            priority: 'Medium',
            paymentDueMode: 'PayLater',
            paymentMethod: 'PaymentLink'
          });
          this.selectedService = null;
        },
        error: (error) => {
          this.isSubmitting = false;
          this.errorMessage = error.error?.message || 'Failed to submit service request. Please try again.';
        }
      });
    } else {
      this.markFormGroupTouched();
    }
  }

  private markFormGroupTouched(): void {
    Object.keys(this.serviceRequestForm.controls).forEach(key => {
      const control = this.serviceRequestForm.get(key);
      control?.markAsTouched();
    });
  }

  getFieldError(fieldName: string): string {
    const control = this.serviceRequestForm.get(fieldName);
    if (control?.errors && control.touched) {
      if (control.errors['required']) {
        return `${this.getFieldLabel(fieldName)} is required`;
      }
      if (control.errors['email']) {
        return 'Please enter a valid email address';
      }
      if (control.errors['pattern']) {
        return 'Please enter a valid phone number';
      }
    }
    return '';
  }

  private getFieldLabel(fieldName: string): string {
    const labels: { [key: string]: string } = {
      serviceType: 'Service Type',
      priority: 'Priority',
      vehicleType: 'Vehicle Type',
      vehicleMake: 'Vehicle Make',
      vehicleModel: 'Vehicle Model',
      vehicleYear: 'Vehicle Year',
      vehicleColor: 'Vehicle Color',
      licensePlate: 'License Plate',
      vin: 'VIN',
      pickupAddress: 'Pickup Address',
      pickupCity: 'Pickup City',
      pickupState: 'Pickup State',
      pickupZipCode: 'Pickup ZIP Code',
      destinationAddress: 'Destination Address',
      destinationCity: 'Destination City',
      destinationState: 'Destination State',
      destinationZipCode: 'Destination ZIP Code',
      description: 'Description',
      contactName: 'Contact Name',
      contactPhone: 'Contact Phone',
      contactEmail: 'Contact Email',
      paymentDueMode: 'Payment Due Mode',
      paymentMethod: 'Payment Method',
      preferredContactTime: 'Preferred Contact Time',
      additionalNotes: 'Additional Notes'
    };
    return labels[fieldName] || fieldName;
  }

  getEstimatedAmount(): number {
    return this.selectedService?.startingPrice ?? 75;
  }

  onPaymentDueModeChange(): void {
    const paymentDueMode = this.serviceRequestForm.get('paymentDueMode')?.value;
    if (paymentDueMode === 'PayNow') {
      this.serviceRequestForm.patchValue({ paymentMethod: 'Card' });
      this.lastAuthorizationAmount = Math.max(150, Math.min(300, this.getEstimatedAmount()));
      return;
    }

    if (paymentDueMode === 'PayLater' && this.serviceRequestForm.get('paymentMethod')?.value === 'Card') {
      this.serviceRequestForm.patchValue({ paymentMethod: 'PaymentLink' });
    }
  }

  private async processPayNowResponse(response: CreateOrderResponse): Promise<void> {
    this.isProcessingPayment = true;
    this.errorMessage = '';

    try {
      if (!response.clientSecret || !response.publishableKey) {
        throw new Error('Pay-now session created but payment details are missing.');
      }

      const stripe = await this.paymentService.getStripe(response.publishableKey);
      if (!stripe) {
        throw new Error('Failed to initialize Stripe.');
      }

      // Minimal card confirmation path. Frontend card element integration can be layered next.
      const result = await stripe.confirmCardPayment(response.clientSecret, {
        payment_method: {
          card: { token: 'tok_visa' }
        }
      } as any);

      if (result.error) {
        throw new Error(result.error.message || 'Payment failed. Please try again or choose Pay Later.');
      }

      const intentStatus = result.paymentIntent?.status;
      if (response.isPreAuthorization && intentStatus === 'requires_capture') {
        this.lastAuthorizationAmount = response.authorizedAmount || response.amount;
        this.successMessage = `Service request submitted. Card pre-authorization hold of $${(this.lastAuthorizationAmount || 0).toFixed(2)} is approved and will be finalized after service completion.`;
      } else {
        this.successMessage = 'Service request submitted and payment processed successfully.';
      }
      this.serviceRequestForm.reset({
        priority: 'Medium',
        paymentDueMode: 'PayLater',
        paymentMethod: 'PaymentLink'
      });
      this.selectedService = null;
    } catch (error: any) {
      this.errorMessage = error?.message || 'Payment initialization failed. Please choose Pay Later.';
    } finally {
      this.isSubmitting = false;
      this.isProcessingPayment = false;
    }
  }

  getPreAuthorizationDisplayAmount(): number {
    const estimate = this.getEstimatedAmount();
    return Math.max(150, Math.min(300, estimate));
  }
}
