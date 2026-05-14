import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { QuotePrintData } from '../components/dashboard/shared/quote-print/quote-print.component';

export interface QuoteEmailRequest {
  toEmail: string;
  subject?: string | null;
  message?: string | null;
  quote: QuotePrintData;
}

export interface QuoteEmailResponse {
  success: boolean;
  toEmail?: string | null;
  postmarkMessageId?: string | null;
  errorMessage?: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class QuoteService {
  constructor(private apiService: ApiService) {}

  /**
   * Sends the quote to the recipient via the configured Postmark sender. The PDF
   * attachment is rendered server-side (Playwright/Chromium) so the frontend only
   * forwards the structured quote data — no html2canvas/jsPDF involvement.
   */
  sendQuoteEmail(payload: QuoteEmailRequest): Observable<QuoteEmailResponse> {
    return this.apiService.post<QuoteEmailResponse>('quotes/email', payload);
  }
}
