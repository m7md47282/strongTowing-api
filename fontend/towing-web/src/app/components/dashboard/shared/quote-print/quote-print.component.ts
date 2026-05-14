import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  input,
  ViewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';

export interface QuotePrintLineItem {
  description: string;
  subtitle?: string;
  quantity: string;
  unitPrice: string;
  amount: string;
}

export interface QuotePrintData {
  quoteRef: string;
  quoteDateDisplay: string;
  validUntilDisplay: string;
  companyName: string;
  companyAddress: string;
  companyPhone: string;
  companyEmail: string;
  companyWebsite: string;
  clientName: string;
  clientPhone: string;
  /** Single-line client for QUOTE FOR card */
  clientDisplayLine: string;
  serviceType: string;
  serviceDateDisplay: string;
  truckTypeLabel: string;
  pickup: string;
  destination: string;
  loadedMileageDisplay: string;
  lineItems: QuotePrintLineItem[];
  totalsSubtotal: string;
  totalsTaxLabel: string;
  totalsTax: string;
  taxPercent: string;
  totalAmount: string;
  notesPreview: string;
}

@Component({
  selector: 'app-quote-print',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './quote-print.component.html',
  styleUrl: './quote-print.component.scss'
})
export class QuotePrintComponent {
  data = input.required<QuotePrintData>();

  @ViewChild('quoteSheet') quoteSheetRef?: ElementRef<HTMLElement>;

  get sheet(): HTMLElement | undefined {
    return this.quoteSheetRef?.nativeElement;
  }

  /** Returns 'Within Service Area' or '—' based on loaded mileage */
  get serviceLocationLabel(): string {
    const val = this.data().loadedMileageDisplay;
    if (!val || val === '—') {
      return '—';
    }
    const miles = parseFloat(val);
    if (isNaN(miles)) {
      return 'Within Service Area';
    }
    return miles <= 50 ? 'Within Service Area' : 'Outside Service Area';
  }
}
