import { Injectable } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiService } from './api.service';
import {
  CreateInvoicePayload,
  InvoiceDetail,
  InvoiceImage,
  InvoiceLineItem,
  InvoiceListItem,
  PagedInvoicesResponse,
  UpdateInvoicePayload
} from '../models/invoice.model';

export interface InvoiceListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: string;
  from?: string;
  to?: string;
}

@Injectable({
  providedIn: 'root'
})
export class InvoiceService {
  constructor(private api: ApiService) {}

  list(params: InvoiceListParams = {}): Observable<PagedInvoicesResponse> {
    let httpParams = new HttpParams();
    const page = params.page ?? 1;
    const pageSize = params.pageSize ?? 20;
    httpParams = httpParams.set('page', String(page)).set('pageSize', String(pageSize));
    if (params.search?.trim()) {
      httpParams = httpParams.set('search', params.search.trim());
    }
    if (params.status && params.status !== 'All') {
      httpParams = httpParams.set('status', params.status);
    }
    if (params.from) {
      httpParams = httpParams.set('from', params.from);
    }
    if (params.to) {
      httpParams = httpParams.set('to', params.to);
    }

    return this.api.get<Record<string, unknown>>('invoices', httpParams).pipe(
      map((raw) => {
        const pageNumber = Number(raw['pageNumber'] ?? raw['PageNumber'] ?? page);
        const pageSizeResolved = Number(raw['pageSize'] ?? raw['PageSize'] ?? pageSize);
        const totalCount = Number(raw['totalCount'] ?? raw['TotalCount'] ?? 0);
        const totalPagesFromApi = Number(raw['totalPages'] ?? raw['TotalPages'] ?? 0);
        const totalPages =
          totalPagesFromApi > 0
            ? totalPagesFromApi
            : totalCount > 0
              ? Math.ceil(totalCount / Math.max(1, pageSizeResolved))
              : 0;
        const hasPreviousPage = pageNumber > 1 && totalCount > 0;
        const hasNextPage = totalPages > 0 ? pageNumber < totalPages : false;

        return {
          data: (Array.isArray(raw['data']) ? raw['data'] : []).map((row) =>
            this.mapListItem(row as Record<string, unknown>)
          ),
          pageNumber,
          pageSize: pageSizeResolved,
          totalCount,
          totalPages,
          hasPreviousPage,
          hasNextPage
        };
      })
    );
  }

  getById(id: number): Observable<InvoiceDetail> {
    return this.api.get<Record<string, unknown>>(`invoices/${id}`).pipe(map((raw) => this.mapDetail(raw)));
  }

  create(payload: CreateInvoicePayload): Observable<InvoiceDetail> {
    return this.api.post<Record<string, unknown>>('invoices', payload).pipe(map((raw) => this.mapDetail(raw)));
  }

  update(id: number, payload: UpdateInvoicePayload): Observable<InvoiceDetail> {
    return this.api.put<Record<string, unknown>>(`invoices/${id}`, payload).pipe(map((raw) => this.mapDetail(raw)));
  }

  updateStatus(id: number, status: string): Observable<{ id: number; status: string }> {
    return this.api.patch<{ id: number; status: string }>(`invoices/${id}/status`, { status });
  }

  delete(id: number): Observable<{ message: string }> {
    return this.api.delete<{ message: string }>(`invoices/${id}`);
  }

  /** Multipart upload; returns full invoice (same shape as getById). */
  uploadImage(invoiceId: number, file: File): Observable<InvoiceDetail> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.api.uploadFile(`invoices/${invoiceId}/images`, formData).pipe(
      map((raw) => this.mapDetail(raw as Record<string, unknown>))
    );
  }

  deleteImage(invoiceId: number, imageId: number): Observable<{ message: string }> {
    return this.api.delete<{ message: string }>(`invoices/${invoiceId}/images/${imageId}`);
  }

  /** Multipart upload; sets custom logo and returns full invoice. */
  uploadLogo(invoiceId: number, file: File): Observable<InvoiceDetail> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.api.uploadFile(`invoices/${invoiceId}/logo`, formData).pipe(
      map((raw) => this.mapDetail(raw as Record<string, unknown>))
    );
  }

  /** Remove custom logo file; default logo is used when the invoice is not set to hide logo. */
  deleteLogo(invoiceId: number): Observable<InvoiceDetail> {
    return this.api.delete<Record<string, unknown>>(`invoices/${invoiceId}/logo`).pipe(
      map((raw) => this.mapDetail(raw))
    );
  }

  private mapListItem(raw: Record<string, unknown>): InvoiceListItem {
    return {
      id: Number(raw['id'] ?? raw['Id']),
      invoiceNumber: String(raw['invoiceNumber'] ?? raw['InvoiceNumber'] ?? ''),
      issuedDate: String(raw['issuedDate'] ?? raw['IssuedDate'] ?? ''),
      dueDate: raw['dueDate'] != null || raw['DueDate'] != null ? String(raw['dueDate'] ?? raw['DueDate']) : null,
      clientName: String(raw['clientName'] ?? raw['ClientName'] ?? ''),
      clientPhone: (raw['clientPhone'] ?? raw['ClientPhone']) != null ? String(raw['clientPhone'] ?? raw['ClientPhone']) : null,
      total: Number(raw['total'] ?? raw['Total'] ?? 0),
      status: String(raw['status'] ?? raw['Status'] ?? ''),
      createdByName: (raw['createdByName'] ?? raw['CreatedByName']) != null ? String(raw['createdByName'] ?? raw['CreatedByName']) : null,
      createdAt: String(raw['createdAt'] ?? raw['CreatedAt'] ?? ''),
      lineItemCount: Number(raw['lineItemCount'] ?? raw['LineItemCount'] ?? 0)
    };
  }

  private mapDetail(raw: Record<string, unknown>): InvoiceDetail {
    const lineRaw = raw['lineItems'] ?? raw['LineItems'];
    const lines = Array.isArray(lineRaw) ? lineRaw : [];
    const imgRaw = raw['images'] ?? raw['Images'];
    const imgs = Array.isArray(imgRaw) ? imgRaw : [];
    return {
      id: Number(raw['id'] ?? raw['Id']),
      invoiceNumber: String(raw['invoiceNumber'] ?? raw['InvoiceNumber'] ?? ''),
      issuedDate: String(raw['issuedDate'] ?? raw['IssuedDate'] ?? ''),
      dueDate: raw['dueDate'] != null || raw['DueDate'] != null ? String(raw['dueDate'] ?? raw['DueDate']) : null,
      clientName: String(raw['clientName'] ?? raw['ClientName'] ?? ''),
      clientPhone: (raw['clientPhone'] ?? raw['ClientPhone']) != null ? String(raw['clientPhone'] ?? raw['ClientPhone']) : null,
      clientEmail: (raw['clientEmail'] ?? raw['ClientEmail']) != null ? String(raw['clientEmail'] ?? raw['ClientEmail']) : null,
      clientAddress: (raw['clientAddress'] ?? raw['ClientAddress']) != null ? String(raw['clientAddress'] ?? raw['ClientAddress']) : null,
      jobId: raw['jobId'] != null || raw['JobId'] != null ? Number(raw['jobId'] ?? raw['JobId']) : null,
      taxRate: Number(raw['taxRate'] ?? raw['TaxRate'] ?? 0),
      subTotal: Number(raw['subTotal'] ?? raw['SubTotal'] ?? 0),
      taxAmount: Number(raw['taxAmount'] ?? raw['TaxAmount'] ?? 0),
      total: Number(raw['total'] ?? raw['Total'] ?? 0),
      notes: (raw['notes'] ?? raw['Notes']) != null ? String(raw['notes'] ?? raw['Notes']) : null,
      status: String(raw['status'] ?? raw['Status'] ?? ''),
      hideLogo: Boolean(raw['hideLogo'] ?? raw['HideLogo']),
      customLogoUrl:
        (raw['customLogoUrl'] ?? raw['CustomLogoUrl']) != null
          ? String(raw['customLogoUrl'] ?? raw['CustomLogoUrl'])
          : null,
      hideCompanyName: Boolean(raw['hideCompanyName'] ?? raw['HideCompanyName']),
      companyDisplayName:
        (raw['companyDisplayName'] ?? raw['CompanyDisplayName']) != null
          ? String(raw['companyDisplayName'] ?? raw['CompanyDisplayName'])
          : null,
      createdById: (raw['createdById'] ?? raw['CreatedById']) != null ? String(raw['createdById'] ?? raw['CreatedById']) : null,
      createdByName: (raw['createdByName'] ?? raw['CreatedByName']) != null ? String(raw['createdByName'] ?? raw['CreatedByName']) : null,
      createdAt: String(raw['createdAt'] ?? raw['CreatedAt'] ?? ''),
      updatedAt: String(raw['updatedAt'] ?? raw['UpdatedAt'] ?? ''),
      lineItems: lines.map((li) => this.mapLineItem(li as Record<string, unknown>)),
      images: imgs.map((img) => this.mapImage(img as Record<string, unknown>))
    };
  }

  private mapImage(raw: Record<string, unknown>): InvoiceImage {
    return {
      id: Number(raw['id'] ?? raw['Id']),
      invoiceId: Number(raw['invoiceId'] ?? raw['InvoiceId']),
      imageUrl: String(raw['imageUrl'] ?? raw['ImageUrl'] ?? ''),
      sortOrder: Number(raw['sortOrder'] ?? raw['SortOrder'] ?? 0),
      uploadedAt: String(raw['uploadedAt'] ?? raw['UploadedAt'] ?? '')
    };
  }

  private mapLineItem(raw: Record<string, unknown>): InvoiceLineItem {
    return {
      id: Number(raw['id'] ?? raw['Id']),
      invoiceId: Number(raw['invoiceId'] ?? raw['InvoiceId']),
      sortOrder: Number(raw['sortOrder'] ?? raw['SortOrder'] ?? 0),
      serviceName: String(raw['serviceName'] ?? raw['ServiceName'] ?? ''),
      details: (raw['details'] ?? raw['Details']) != null ? String(raw['details'] ?? raw['Details']) : null,
      category: String(raw['category'] ?? raw['Category'] ?? ''),
      unitType: (raw['unitType'] ?? raw['UnitType']) != null ? String(raw['unitType'] ?? raw['UnitType']) : null,
      unitPrice: Number(raw['unitPrice'] ?? raw['UnitPrice'] ?? 0),
      quantity: Number(raw['quantity'] ?? raw['Quantity'] ?? 0),
      discount: Number(raw['discount'] ?? raw['Discount'] ?? 0),
      isDiscountPercentage: Boolean(raw['isDiscountPercentage'] ?? raw['IsDiscountPercentage']),
      isTaxable: Boolean(raw['isTaxable'] ?? raw['IsTaxable']),
      total: Number(raw['total'] ?? raw['Total'] ?? 0)
    };
  }
}
