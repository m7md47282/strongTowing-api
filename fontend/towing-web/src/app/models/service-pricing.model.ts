export interface ServicePricingProfile {
  id: number;
  name: string;
  basePrice: number;
  pricePerMile: number;
  isAvailable: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateServicePricingProfilePayload {
  name: string;
  basePrice: number;
  pricePerMile: number;
  isAvailable: boolean;
}

export type UpdateServicePricingProfilePayload = CreateServicePricingProfilePayload;
