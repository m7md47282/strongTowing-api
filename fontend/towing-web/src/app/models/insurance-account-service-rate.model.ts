export interface InsuranceAccountServiceRate {
  id: number;
  insuranceAccountId: number;
  servicePricingProfileId: number;
  serviceName: string;
  basePrice: number;
  pricePerMile: number;
  createdAt: string;
  updatedAt: string;
}

export interface UpsertInsuranceAccountServiceRatePayload {
  servicePricingProfileId: number;
  basePrice: number;
  pricePerMile: number;
}
