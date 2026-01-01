import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { OrderService } from '../../services/order.service';
import { CreateOrderRequest } from '../../models/order.model';

interface Service {
  id: number;
  name: string;
  description: string;
  icon: string;
  features: string[];
  startingPrice: number;
  priceNote: string;
}

interface ServiceCategory {
  name: string;
  description: string;
  image: string;
}

interface ServiceArea {
  state: string;
  cities: string[];
}

interface WhyChooseUsFeature {
  title: string;
  description: string;
  icon: string;
}

interface PhotoGallery {
  image: string;
  alt: string;
}

@Component({
  selector: 'app-services',
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './services.component.html',
  styleUrls: ['./services.component.scss']
})
export class ServicesComponent implements OnInit {
  serviceRequestForm: FormGroup;
  isSubmitting = false;
  successMessage = '';
  errorMessage = '';

  services: Service[] = [
    {
      id: 1,
      name: 'Towing Services',
      description: 'Professional towing for all vehicle types with state-of-the-art equipment.',
      icon: 'fas fa-truck',
      features: [
        'Light-duty towing',
        'Heavy-duty towing',
        'Motorcycle towing',
        'RV towing',
        'Flatbed towing'
      ],
      startingPrice: 75,
      priceNote: 'First 10 miles included'
    },
    {
      id: 2,
      name: 'Roadside Assistance',
      description: 'Quick and reliable roadside assistance to get you back on the road.',
      icon: 'fas fa-tools',
      features: [
        'Jump start service',
        'Tire change',
        'Lockout service',
        'Fuel delivery',
        'Battery replacement'
      ],
      startingPrice: 50,
      priceNote: 'Basic service fee'
    },
    {
      id: 3,
      name: 'Emergency Services',
      description: '24/7 emergency towing and roadside assistance when you need it most.',
      icon: 'fas fa-exclamation-triangle',
      features: [
        '24/7 availability',
        'Fast response time',
        'Emergency dispatch',
        'Accident recovery',
        'Insurance coordination'
      ],
      startingPrice: 100,
      priceNote: 'Emergency rates apply'
    },
    {
      id: 4,
      name: 'Long Distance Towing',
      description: 'Reliable long-distance towing services across state lines.',
      icon: 'fas fa-route',
      features: [
        'Interstate towing',
        'Cross-country transport',
        'Vehicle shipping',
        'Insurance coordination',
        'Progress tracking'
      ],
      startingPrice: 200,
      priceNote: 'Per mile pricing'
    },
    {
      id: 5,
      name: 'Specialty Towing',
      description: 'Specialized towing for unique vehicles and situations.',
      icon: 'fas fa-car-crash',
      features: [
        'Accident recovery',
        'Rollover recovery',
        'Water recovery',
        'Off-road recovery',
        'Motorcycle recovery'
      ],
      startingPrice: 150,
      priceNote: 'Specialty rates apply'
    },
    {
      id: 6,
      name: 'Fleet Services',
      description: 'Comprehensive towing and maintenance services for fleet vehicles.',
      icon: 'fas fa-truck-moving',
      features: [
        'Fleet management',
        'Scheduled maintenance',
        'Emergency response',
        'Driver assistance',
        'Cost tracking'
      ],
      startingPrice: 500,
      priceNote: 'Monthly contract rates'
    }
  ];

  serviceCategories: ServiceCategory[] = [
    {
      name: 'Personal Vehicles',
      description: 'Towing and roadside assistance for personal cars, trucks, and motorcycles.',
      image: '/images/photage/photage-6.jpeg'
    },
    {
      name: 'Commercial Vehicles',
      description: 'Professional towing services for commercial trucks and fleet vehicles.',
      image: '/images/photage/photage-7.jpeg'
    },
    {
      name: 'Emergency Response',
      description: '24/7 emergency towing and recovery services for urgent situations.',
      image: '/images/photage/photage-8.jpeg'
    },
    {
      name: 'Specialty Transport',
      description: 'Specialized towing for unique vehicles and challenging situations.',
      image: '/images/photage/photage-9.jpeg'
    }
  ];

  serviceAreas: ServiceArea[] = [
    {
      state: 'Virginia',
      cities: ['Alexandria', 'Arlington', 'Fairfax', 'Richmond', 'Norfolk', 'Virginia Beach']
    },
    {
      state: 'Maryland',
      cities: ['Baltimore', 'Annapolis', 'Rockville', 'Frederick', 'Gaithersburg', 'Silver Spring']
    },
    {
      state: 'Washington DC',
      cities: ['Washington', 'Georgetown', 'Capitol Hill', 'Dupont Circle', 'Adams Morgan']
    }
  ];

  whyChooseUs: WhyChooseUsFeature[] = [
    {
      title: '24/7 Availability',
      description: 'We\'re always here when you need us, day or night, weekends and holidays.',
      icon: 'fas fa-clock'
    },
    {
      title: 'Fast Response',
      description: 'Our average response time is under 30 minutes in most areas.',
      icon: 'fas fa-tachometer-alt'
    },
    {
      title: 'Professional Team',
      description: 'Licensed, insured, and experienced drivers with top-notch equipment.',
      icon: 'fas fa-user-tie'
    },
    {
      title: 'Competitive Pricing',
      description: 'Fair, transparent pricing with no hidden fees or surprise charges.',
      icon: 'fas fa-dollar-sign'
    },
    {
      title: 'Fully Insured',
      description: 'Comprehensive insurance coverage for your peace of mind.',
      icon: 'fas fa-shield-alt'
    },
    {
      title: 'Modern Equipment',
      description: 'State-of-the-art towing trucks and equipment for safe transport.',
      icon: 'fas fa-tools'
    }
  ];

  photoGallery: PhotoGallery[] = [
    { image: "/images/photage/photage-1.jpeg", alt: "Professional Towing Service" },
    { image: "/images/photage/photage-2.jpeg", alt: "Roadside Assistance" },
    { image: "/images/photage/photage-3.jpeg", alt: "Emergency Recovery" },
    { image: "/images/photage/photage-4.jpeg", alt: "Commercial Vehicle Towing" }
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
      contactPhone: ['', [Validators.required, Validators.pattern(/^[\d\s\-\+\(\)]+$/)]]
    });
  }

  ngOnInit(): void {
    // Component initialization
  }

  requestService(service: Service): void {
    // Pre-fill the form with the selected service
    this.serviceRequestForm.patchValue({
      serviceType: service.id,
      description: `Request for ${service.name} service`
    });
    
    // Scroll to the form
    this.scrollToForm();
  }

  scrollToForm(): void {
    const formElement = document.querySelector('.request-service-form');
    if (formElement) {
      formElement.scrollIntoView({ 
        behavior: 'smooth',
        block: 'start'
      });
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
        notes: `Contact phone: ${formValue.contactPhone}`
      };

      this.orderService.createOrder(orderRequest).subscribe({
        next: (response) => {
          this.isSubmitting = false;
          this.successMessage = 'Service request submitted successfully! We\'ll contact you shortly.';
          this.serviceRequestForm.reset();
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
}