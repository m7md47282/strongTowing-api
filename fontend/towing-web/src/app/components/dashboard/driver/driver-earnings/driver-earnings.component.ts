import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs/operators';
import { DriversService, DriverEarningsSummary } from '../../../../services/drivers.service';
import { parseApiError } from '../../../../utils/api-error.util';

@Component({
  selector: 'app-driver-earnings',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './driver-earnings.component.html',
  styleUrl: './driver-earnings.component.scss'
})
export class DriverEarningsComponent implements OnInit {
  loading = true;
  error: string | null = null;
  data: DriverEarningsSummary | null = null;

  constructor(private driversService: DriversService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = null;
    this.driversService
      .getMyEarnings()
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (d) => (this.data = d),
        error: (err) => {
          this.error = parseApiError(err);
        }
      });
  }
}
