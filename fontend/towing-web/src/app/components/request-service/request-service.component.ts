import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { OrderService } from '../../services/order.service';
import { CreateOrderRequest } from '../../models/order.model';

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
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './request-service.component.html',
  styleUrls: ['./request-service.component.scss']
})
export class RequestServiceComponent implements OnInit {
  serviceRequestForm: FormGroup;
  isSubmitting = false;
  successMessage = '';
  errorMessage = '';
  selectedService: ServiceType | null = null;
  currentYear = new Date().getFullYear();
  maxYear = this.currentYear + 1;

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
    private orderService: OrderService
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
      contactPhone: ['', [Validators.required, Validators.pattern(/^[\d\s\-\+\(\)]+$/)]],
      contactEmail: ['', [Validators.email]],
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
        notes: `Contact phone: ${formValue.contactPhone}${formValue.contactEmail ? `, Email: ${formValue.contactEmail}` : ''}${formValue.preferredContactTime ? `, Preferred contact time: ${formValue.preferredContactTime}` : ''}${formValue.additionalNotes ? `, Additional notes: ${formValue.additionalNotes}` : ''}`
      };

      this.orderService.createOrder(orderRequest).subscribe({
        next: (response) => {
          this.isSubmitting = false;
          this.successMessage = 'Service request submitted successfully! We\'ll contact you shortly.';
          this.serviceRequestForm.reset();
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
      contactPhone: 'Contact Phone',
      contactEmail: 'Contact Email',
      preferredContactTime: 'Preferred Contact Time',
      additionalNotes: 'Additional Notes'
    };
    return labels[fieldName] || fieldName;
  }
}
