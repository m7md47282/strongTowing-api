export interface ServicePricingProfile {
  id: number;
  name: string;
  loadedPrice: number;
  deadHeadPrice: number;
  isAvailable: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateServicePricingProfilePayload {
  name: string;
  loadedPrice: number;
  deadHeadPrice: number;
  isAvailable: boolean;
}

export type UpdateServicePricingProfilePayload = CreateServicePricingProfilePayload;
