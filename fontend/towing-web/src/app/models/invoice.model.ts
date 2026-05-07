export interface InvoiceListItem {
  id: number;
  invoiceNumber: string;
  issuedDate: string;
  dueDate: string | null;
  clientName: string;
  clientPhone: string | null;
  total: number;
  status: string;
  createdByName: string | null;
  createdAt: string;
  lineItemCount: number;
}

export interface InvoiceImage {
  id: number;
  invoiceId: number;
  imageUrl: string;
  sortOrder: number;
  uploadedAt: string;
}

export interface InvoiceLineItem {
  id: number;
  invoiceId: number;
  sortOrder: number;
  serviceName: string;
  details: string | null;
  category: string;
  unitType: string | null;
  unitPrice: number;
  quantity: number;
  discount: number;
  isDiscountPercentage: boolean;
  isTaxable: boolean;
  total: number;
}

export interface InvoiceDetail {
  id: number;
  invoiceNumber: string;
  issuedDate: string;
  dueDate: string | null;
  clientName: string;
  clientPhone: string | null;
  clientEmail: string | null;
  clientAddress: string | null;
  jobId: number | null;
  taxRate: number;
  subTotal: number;
  taxAmount: number;
  total: number;
  notes: string | null;
  status: string;
  createdById: string | null;
  createdByName: string | null;
  createdAt: string;
  updatedAt: string;
  lineItems: InvoiceLineItem[];
  images: InvoiceImage[];
}

export interface PagedInvoicesResponse {
  data: InvoiceListItem[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface CreateInvoiceLinePayload {
  serviceName: string;
  details?: string | null;
  category: string;
  unitType?: string | null;
  unitPrice: number;
  quantity: number;
  discount: number;
  isDiscountPercentage: boolean;
  isTaxable: boolean;
  sortOrder: number;
}

export interface CreateInvoicePayload {
  issuedDate: string;
  dueDate?: string | null;
  clientName: string;
  clientPhone?: string | null;
  clientEmail?: string | null;
  clientAddress?: string | null;
  jobId?: number | null;
  taxRate: number;
  notes?: string | null;
  status: string;
  lineItems: CreateInvoiceLinePayload[];
}

/** Same shape as create; used for PUT /api/invoices/{id}. */
export type UpdateInvoicePayload = CreateInvoicePayload;
