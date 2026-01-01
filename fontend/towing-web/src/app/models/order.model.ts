import { User, Driver, Customer } from './user.model';

export interface Order {
  id: number;
  customerId: number;
  driverId?: number;
  serviceType: string;
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
  pickupLatitude?: number;
  pickupLongitude?: number;
  destinationAddress: string;
  destinationCity: string;
  destinationState: string;
  destinationZipCode: string;
  destinationLatitude?: number;
  destinationLongitude?: number;
  description: string;
  estimatedCost: number;
  actualCost?: number;
  status: OrderStatus;
  priority: OrderPriority;
  scheduledDate?: Date;
  completedDate?: Date;
  createdAt: Date;
  updatedAt: Date;
  customer?: Customer;
  driver?: Driver;
  notes?: string;
  images?: string[];
}

export enum OrderStatus {
  Pending = 'Pending',
  Assigned = 'Assigned',
  InProgress = 'InProgress',
  Completed = 'Completed',
  Cancelled = 'Cancelled'
}

export enum OrderPriority {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
  Emergency = 'Emergency'
}

export interface CreateOrderRequest {
  serviceType: string;
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
  destinationAddress: string;
  destinationCity: string;
  destinationState: string;
  destinationZipCode: string;
  description: string;
  priority: OrderPriority;
  scheduledDate?: Date;
  notes?: string;
}

export interface OrderTracking {
  orderId: number;
  status: OrderStatus;
  location?: {
    latitude: number;
    longitude: number;
    address: string;
  };
  estimatedArrival?: Date;
  driver?: {
    id: number;
    name: string;
    phone: string;
    vehicle: string;
  };
  lastUpdated: Date;
}

export interface OrderFilter {
  status?: OrderStatus;
  priority?: OrderPriority;
  serviceType?: string;
  dateFrom?: Date;
  dateTo?: Date;
  customerId?: number;
  driverId?: number;
  page?: number;
  pageSize?: number;
}
