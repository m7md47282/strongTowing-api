import { Component, OnInit } from '@angular/core';
import { RouterModule, Router, NavigationEnd } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../services/auth.service';
import { SIDEBAR_MENU_ITEMS, MenuItem } from '../../../constants/sidebar-menu.constants';
import { ROLE_LABELS, RoleId } from '../../../constants/user-roles.constants';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterModule, FormsModule],
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.scss']
})
export class SidebarComponent implements OnInit {
  menuItems: MenuItem[] = [];
  currentRoute: string = '';
  expandedItems: Set<string> = new Set();
  darkMode: boolean = false;

  constructor(
    public authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.filterMenuItems();
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe((event: any) => {
        this.currentRoute = event.url;
        this.autoExpandActiveItems();
      });
    this.currentRoute = this.router.url;
    this.autoExpandActiveItems();
  }

  autoExpandActiveItems(): void {
    this.menuItems.forEach(item => {
      if (item.children && this.hasActiveChild(item)) {
        this.expandedItems.add(item.route);
      }
    });
  }

  filterMenuItems(): void {
    const user = this.authService.getCurrentUser();
    if (!user) {
      this.menuItems = [];
      return;
    }

    // Ensure roleId is a number for comparison (handle both string and number)
    const userRoleId = typeof user.roleId === 'string' ? parseInt(user.roleId, 10) : Number(user.roleId);

    this.menuItems = SIDEBAR_MENU_ITEMS
      .filter(item => {
        // Check if user's role is in the allowed roles array
        return item.roles.some(role => role === userRoleId);
      })
      .map(item => ({
        ...item,
        children: item.children?.filter(child => child.roles.some(role => role === userRoleId))
      }));
  }

  toggleSubmenu(item: MenuItem): void {
    if (item.children && item.children.length > 0) {
      if (this.expandedItems.has(item.route)) {
        this.expandedItems.delete(item.route);
      } else {
        this.expandedItems.add(item.route);
      }
    }
  }

  isExpanded(item: MenuItem): boolean {
    return this.expandedItems.has(item.route);
  }

  hasActiveChild(item: MenuItem): boolean {
    if (!item.children) return false;
    return item.children.some(child => this.currentRoute.startsWith(child.route));
  }

  getRoleLabel(): string {
    const user = this.authService.getCurrentUser();
    if (!user) return '';
    return ROLE_LABELS[user.roleId as RoleId] || 'User';
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => {
        this.router.navigate(['/login']);
      },
      error: (error) => {
        // Even if logout API fails, navigate to login (local storage already cleared)
        this.router.navigate(['/login']);
      }
    });
  }

  getInitials(): string {
    const user = this.authService.getCurrentUser();
    if (!user || !user.fullName) return 'U';
    const names = user.fullName.split(' ');
    if (names.length >= 2) {
      return (names[0][0] + names[1][0]).toUpperCase();
    }
    return user.fullName[0].toUpperCase();
  }
}

