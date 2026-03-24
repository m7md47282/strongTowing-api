export interface InsuranceAccount {
  id: number;
  name: string;
  accountNumber: string | null;
  contactName: string | null;
  billingEmail: string | null;
  billingPhone: string | null;
  addressLine1: string | null;
  addressLine2: string | null;
  city: string | null;
  state: string | null;
  postalCode: string | null;
  notes: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateInsuranceAccountPayload {
  name: string;
  accountNumber?: string | null;
  contactName?: string | null;
  billingEmail?: string | null;
  billingPhone?: string | null;
  addressLine1?: string | null;
  addressLine2?: string | null;
  city?: string | null;
  state?: string | null;
  postalCode?: string | null;
  notes?: string | null;
  isActive: boolean;
}

export type UpdateInsuranceAccountPayload = CreateInsuranceAccountPayload;
