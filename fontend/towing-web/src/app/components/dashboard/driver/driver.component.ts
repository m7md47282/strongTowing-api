import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../../shared/sidebar/sidebar.component';
import { NotificationBellComponent } from '../../shared/notification-bell/notification-bell.component';

@Component({
  selector: 'app-driver',
  standalone: true,
  imports: [CommonModule, RouterOutlet, SidebarComponent, NotificationBellComponent],
  templateUrl: './driver.component.html',
  styleUrl: './driver.component.scss'
})
export class DriverComponent {}
