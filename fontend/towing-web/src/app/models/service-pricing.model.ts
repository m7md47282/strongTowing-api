export interface ServicePricingProfile {
  id: number;
  name: string;
  basePrice: number;
  /** Legacy loaded $/mi; when `loadedPricePerMile` is set it is the explicit loaded rate. */
  pricePerMile: number;
  enroutePricePerMile: number | null;
  loadedPricePerMile: number | null;
  deadheadPricePerMile: number | null;
  isAvailable: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateServicePricingProfilePayload {
  name: string;
  basePrice: number;
  pricePerMile: number;
  enroutePricePerMile?: number | null;
  loadedPricePerMile?: number | null;
  deadheadPricePerMile?: number | null;
  isAvailable: boolean;
}

export type UpdateServicePricingProfilePayload = CreateServicePricingProfilePayload;
