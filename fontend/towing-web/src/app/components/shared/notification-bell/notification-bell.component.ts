import { Component, ElementRef, HostListener, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { InAppNotificationsService, InAppNotification } from '../../../services/in-app-notifications.service';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification-bell.component.html',
  styleUrl: './notification-bell.component.scss'
})
export class NotificationBellComponent {
  private readonly el = inject(ElementRef<HTMLElement>);
  private readonly inApp = inject(InAppNotificationsService);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);

  readonly vm$ = this.inApp.vm$;

  dropdownOpen = false;

  toggle(event: MouseEvent): void {
    event.stopPropagation();
    this.dropdownOpen = !this.dropdownOpen;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.el.nativeElement.contains(event.target as Node)) {
      this.dropdownOpen = false;
    }
  }

  markAllRead(event: MouseEvent): void {
    event.stopPropagation();
    this.inApp.markAllRead();
  }

  onSelect(n: InAppNotification, event: MouseEvent): void {
    event.stopPropagation();
    this.inApp.markRead(n.id);

    const jobId = n.data?.['jobId'];
    if (jobId) {
      if (this.auth.isDriver()) {
        void this.router.navigate(['/driver/jobs'], { queryParams: { jobId } });
      } else if (this.auth.isDispatcher()) {
        void this.router.navigate(['/dispatcher/jobs'], { queryParams: { highlight: jobId } });
      } else if (this.auth.isAdmin()) {
        void this.router.navigate(['/admin/jobs'], { queryParams: { highlight: jobId } });
      }
    }

    this.dropdownOpen = false;
  }
}
