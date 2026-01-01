export interface Service {
  id: number;
  name: string;
  description: string;
  basePrice: number;
  pricePerMile: number;
  isActive: boolean;
  category: ServiceCategory;
  estimatedDuration: number; // in minutes
  requirements: string[];
  createdAt: Date;
  updatedAt: Date;
}

export enum ServiceCategory {
  Towing = 'Towing',
  RoadsideAssistance = 'RoadsideAssistance',
  JumpStart = 'JumpStart',
  TireChange = 'TireChange',
  Lockout = 'Lockout',
  FuelDelivery = 'FuelDelivery',
  Winch = 'Winch',
  Recovery = 'Recovery',
  Other = 'Other'
}

export interface ServiceRequest {
  serviceId: number;
  customerId: number;
  vehicleType: string;
  vehicleMake: string;
  vehicleModel: string;
  vehicleYear: number;
  vehicleColor: string;
  licensePlate: string;
  vin: string;
  pickupAddress: string;
  pickupCity: string;
  pickupState: string;
  pickupZipCode: string;
  destinationAddress?: string;
  destinationCity?: string;
  destinationState?: string;
  destinationZipCode?: string;
  description: string;
  priority: 'Low' | 'Medium' | 'High' | 'Emergency';
  scheduledDate?: Date;
  notes?: string;
}

export interface ServiceProvider {
  id: number;
  companyName: string;
  contactPerson: string;
  email: string;
  phone: string;
  address: string;
  city: string;
  state: string;
  zipCode: string;
  services: Service[];
  isActive: boolean;
  rating: number;
  totalJobs: number;
  createdAt: Date;
  updatedAt: Date;
}

export interface ServiceArea {
  id: number;
  name: string;
  state: string;
  cities: string[];
  zipCodes: string[];
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
}

export interface ServicePricing {
  serviceId: number;
  basePrice: number;
  pricePerMile: number;
  minimumCharge: number;
  maximumCharge?: number;
  weekendMultiplier: number;
  holidayMultiplier: number;
  nightMultiplier: number;
  emergencyMultiplier: number;
  isActive: boolean;
  effectiveDate: Date;
  endDate?: Date;
}
