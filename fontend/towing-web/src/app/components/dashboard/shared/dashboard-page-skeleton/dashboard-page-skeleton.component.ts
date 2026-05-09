import { Component, Input } from '@angular/core';

export type DashboardPageSkeletonVariant = 'admin' | 'compact';

@Component({
  selector: 'app-dashboard-page-skeleton',
  standalone: true,
  templateUrl: './dashboard-page-skeleton.component.html',
  styleUrl: './dashboard-page-skeleton.component.scss'
})
export class DashboardPageSkeletonComponent {
  @Input() variant: DashboardPageSkeletonVariant = 'admin';
}
