import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { InvoiceService } from '../../../../services/invoice.service';
import { resolvePublicAssetUrl } from '../../../../services/job.service';
import { InvoiceDetail } from '../../../../models/invoice.model';
import { CONTACT_INFO } from '../../../../constants/contact-info.constants';

@Component({
  selector: 'app-invoice-print',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './invoice-print.component.html',
  styleUrl: './invoice-print.component.scss'
})
export class InvoicePrintComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly invoiceService = inject(InvoiceService);

  id: number | null = null;
  invoice: InvoiceDetail | null = null;
  loading = true;
  error: string | null = null;
  printUrl = '';
  qrSrc = '';

  readonly contact = CONTACT_INFO;

  /** “FROM” block — matches branded invoice layout. */
  readonly fromLines: readonly string[] = [
    'Strong Towing',
    '(703) 200-8836',
    CONTACT_INFO.email.info,
    'FALLS CHURCH VA 22041-2833',
    '5652 Columbia Pike, Falls Church, VA 22041'
  ];

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    const parsed = idParam ? parseInt(idParam, 10) : NaN;
    if (!Number.isFinite(parsed) || parsed <= 0) {
      this.error = 'Invalid invoice.';
      this.loading = false;
      return;
    }
    this.id = parsed;
    this.printUrl =
      typeof window !== 'undefined' ? `${window.location.origin}/invoice-print/${parsed}` : '';
    this.load();
  }

  load(): void {
    if (this.id == null) {
      return;
    }
    this.invoiceService.getById(this.id).subscribe({
      next: (inv) => {
        this.invoice = inv;
        this.loading = false;
        const enc = encodeURIComponent(this.printUrl);
        this.qrSrc = `https://api.qrserver.com/v1/create-qr-code/?size=140x140&data=${enc}`;
      },
      error: (err: HttpErrorResponse) => {
        this.loading = false;
        const body = err.error as { message?: string } | undefined;
        this.error = body?.message ?? err.message ?? 'Failed to load invoice.';
      }
    });
  }

  formatIssued(iso: string): string {
    const d = new Date(iso);
    if (Number.isNaN(d.getTime())) {
      return '';
    }
    return d.toLocaleDateString('en-GB', { day: '2-digit', month: '2-digit', year: 'numeric' });
  }

  formatMoney(value: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 2
    }).format(value);
  }

  triggerPrint(): void {
    setTimeout(() => window.print(), 200);
  }

  resolveImageUrl(path: string): string {
    return resolvePublicAssetUrl(path);
  }
}
