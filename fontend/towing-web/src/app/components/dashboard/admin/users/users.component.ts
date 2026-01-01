import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../../services/api.service';
import { User } from '../../../../models/user.model';
import { ROLE_LABELS, RoleId } from '../../../../constants/user-roles.constants';

// API Response interface (matches backend UserDto)
interface UserApiResponse {
  id: string;
  email: string;
  fullName: string;
  phoneNumber: string | null;
  role: string; // API returns role as string
  roleId?: number; // May or may not be present
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.scss']
})
export class UsersComponent implements OnInit {
  users: User[] = [];
  loading = false;
  error: string | null = null;
  searchTerm = '';

  // Map role string to roleId
  private roleToRoleIdMap: Record<string, RoleId> = {
    'SuperAdmin': RoleId.SuperAdmin,
    'Administrator': RoleId.Admin,
    'Dispatcher': RoleId.Dispatcher,
    'Driver': RoleId.Driver,
    'User': RoleId.User
  };

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading = true;
    this.error = null;

    this.apiService.get<UserApiResponse[]>('users').subscribe({
      next: (data) => {
        // Map API response to User model
        this.users = data.map(user => this.mapApiResponseToUser(user));
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load users. Please try again.';
        this.loading = false;
        console.error('Error loading users:', err);
      }
    });
  }

  private mapApiResponseToUser(apiUser: UserApiResponse): User {
    // If roleId is already present, use it; otherwise map from role string
    const roleId = apiUser.roleId || this.roleToRoleIdMap[apiUser.role] || RoleId.User;
    
    return {
      id: apiUser.id,
      email: apiUser.email,
      fullName: apiUser.fullName,
      phoneNumber: apiUser.phoneNumber,
      roleId: roleId,
      isActive: apiUser.isActive,
      createdAt: apiUser.createdAt,
      updatedAt: apiUser.updatedAt
    };
  }

  getRoleLabel(roleId: number): string {
    return ROLE_LABELS[roleId as RoleId] || 'Unknown';
  }

  get filteredUsers(): User[] {
    if (!this.searchTerm) {
      return this.users;
    }

    const term = this.searchTerm.toLowerCase();
    return this.users.filter(user =>
      user.fullName.toLowerCase().includes(term) ||
      user.email.toLowerCase().includes(term) ||
      (user.phoneNumber && user.phoneNumber.toLowerCase().includes(term)) ||
      this.getRoleLabel(user.roleId).toLowerCase().includes(term)
    );
  }

  formatDate(dateString: string | null): string {
    if (!dateString) return '-';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}

