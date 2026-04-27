import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpParams } from '@angular/common/http';
import { catchError, finalize } from 'rxjs/operators';
import { of } from 'rxjs';
import { ApiService } from '../../../../services/api.service';
import { AuthService } from '../../../../services/auth.service';
import { User, RegisterRequest } from '../../../../models/user.model';
import { ROLE_LABELS, RoleId, USER_ROLES } from '../../../../constants/user-roles.constants';

interface UserApiResponse {
  id: string;
  email: string;
  fullName: string;
  phoneNumber: string | null;
  role: string;
  roleId?: string | number;
  isActive: boolean;
  hasChangedPassword?: boolean;
  passwordChangedAt?: string | null;
  createdAt: string;
  updatedAt: string | null;
}

interface CreateUserResponse extends UserApiResponse {
  temporaryPassword?: string;
}

interface PagedResponse<T> {
  data: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

interface CreateUserRequest {
  email: string;
  password: string;
  fullName: string;
  role: string;
  phoneNumber?: string | null;
}

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.scss']
})
export class UsersComponent implements OnInit {
  users: User[] = [];
  loading = false;
  error: string | null = null;
  searchTerm = '';
  showCreateModal = false;
  createUserForm: FormGroup;
  submitting = false;
  successMessage: string | null = null;
  editingRoleUserId: string | null = null;
  selectedEditRoleId: RoleId | null = null;
  updatingRole = false;

  deleteConfirmUserId: string | null = null;
  deleteConfirmExpectedName = '';
  deleteConfirmationInput = '';
  removeSubmitting = false;

  /** Target user for activate/deactivate modal (browser confirm removed). */
  statusConfirmUser: User | null = null;
  statusSubmitting = false;
  /** Separate from {@link error} so table stays visible while modal shows failure. */
  statusToggleModalError: string | null = null;

  temporaryPasswords: Map<string, string> = new Map();

  pageNumber: number = 1;
  pageSize: number = 10;
  totalCount: number = 0;
  totalPages: number = 0;
  hasPreviousPage: boolean = false;
  hasNextPage: boolean = false;

  selectedRole: string = '';
  selectedStatus: string = '';

  availableRoles: { id: RoleId; label: string; roleString: string }[] = [];
  
  filterRoles: { value: string; label: string }[] = [
    { value: '', label: 'All Roles' },
    { value: 'SuperAdmin', label: 'Super Admin' },
    { value: 'Administrator', label: 'Administrator' },
    { value: 'Dispatcher', label: 'Dispatcher' },
    { value: 'Driver', label: 'Driver' }
  ];

  private roleToRoleIdMap: Record<string, RoleId> = {
    'SuperAdmin': RoleId.SuperAdmin,
    'Administrator': RoleId.Admin,
    'Dispatcher': RoleId.Dispatcher,
    'Driver': RoleId.Driver,
    'User': RoleId.User
  };

  private roleIdToRoleStringMap: Record<RoleId, string> = {
    [RoleId.SuperAdmin]: 'SuperAdmin',
    [RoleId.Admin]: 'Administrator',
    [RoleId.Dispatcher]: 'Dispatcher',
    [RoleId.Driver]: 'Driver',
    [RoleId.User]: 'User'
  };

  constructor(
    private apiService: ApiService,
    private authService: AuthService,
    private fb: FormBuilder
  ) {
    this.createUserForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      fullName: ['', [Validators.required, Validators.minLength(2)]],
      phoneNumber: [''],
      role: ['', [Validators.required]]
    });
  }

  ngOnInit(): void {
    this.loadUsers();
    this.setupAvailableRoles();
  }

  setupAvailableRoles(): void {
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser) {
      return;
    }

    const userRoleId = typeof currentUser.roleId === 'string' ? parseInt(currentUser.roleId, 10) : Number(currentUser.roleId);

    if (userRoleId === RoleId.SuperAdmin) {
      this.availableRoles = [
        { id: RoleId.SuperAdmin, label: 'Super Admin', roleString: 'SuperAdmin' },
        { id: RoleId.Admin, label: 'Administrator', roleString: 'Administrator' },
        { id: RoleId.Dispatcher, label: 'Dispatcher', roleString: 'Dispatcher' },
        { id: RoleId.Driver, label: 'Driver', roleString: 'Driver' },
        { id: RoleId.User, label: 'User', roleString: 'User' }
      ];
    } else if (userRoleId === RoleId.Admin) {
      this.availableRoles = [
        { id: RoleId.Admin, label: 'Administrator', roleString: 'Administrator' },
        { id: RoleId.Dispatcher, label: 'Dispatcher', roleString: 'Dispatcher' },
        { id: RoleId.Driver, label: 'Driver', roleString: 'Driver' },
        { id: RoleId.User, label: 'User', roleString: 'User' }
      ];
    } else {
      this.availableRoles = [];
    }
  }

  loadUsers(): void {
    this.loading = true;
    this.error = null;

    let params = new HttpParams()
      .set('pageNumber', this.pageNumber.toString())
      .set('pageSize', this.pageSize.toString());

    if (this.selectedRole) {
      params = params.set('role', this.selectedRole);
    }

    if (this.selectedStatus === 'active') {
      params = params.set('isActive', 'true');
    } else if (this.selectedStatus === 'inactive') {
      params = params.set('isActive', 'false');
    }

    if (this.searchTerm && this.searchTerm.trim()) {
      params = params.set('search', this.searchTerm.trim());
    }

    this.apiService.get<PagedResponse<UserApiResponse>>('users', params)
      .pipe(
        catchError((err) => {
          this.error = err.error?.message || err.error?.error || 'Failed to load users. Please try again.';
          this.users = [];
          // Reset pagination on error
          this.totalCount = 0;
          this.totalPages = 0;
          this.hasPreviousPage = false;
          this.hasNextPage = false;
          return of({ data: [], pageNumber: 1, pageSize: 10, totalCount: 0, totalPages: 0, hasPreviousPage: false, hasNextPage: false });
        }),
        finalize(() => {
          this.loading = false;
        })
      )
      .subscribe({
        next: (response) => {
          try {
            if (response && response.data) {
              this.pageNumber = response.pageNumber || this.pageNumber;
              this.pageSize = response.pageSize || this.pageSize;
              this.totalCount = response.totalCount || 0;
              this.totalPages = response.totalPages || 0;
              this.hasPreviousPage = response.hasPreviousPage || false;
              this.hasNextPage = response.hasNextPage || false;

              if (response.data && response.data.length > 0) {
                this.users = response.data.map(user => {
                  const mappedUser = this.mapApiResponseToUser(user);
                  
                  if (user.hasChangedPassword === true) {
                    this.temporaryPasswords.delete(mappedUser.id);
                  }
                  
                  return mappedUser;
                });
              } else {
                this.users = [];
              }
            } else {
              this.users = [];
              this.totalCount = 0;
              this.totalPages = 0;
            }
          } catch (error) {
            this.error = 'Failed to process user data. Please try again.';
            this.users = [];
          }
        }
      });
  }

  private mapApiResponseToUser(apiUser: UserApiResponse): User & { hasChangedPassword?: boolean } {
    try {
      let roleId = RoleId.User;
      
      if (apiUser.roleId !== undefined && apiUser.roleId !== null) {
        const roleIdValue = typeof apiUser.roleId === 'string' ? parseInt(apiUser.roleId, 10) : apiUser.roleId;
        roleId = roleIdValue as RoleId;
      } else if (apiUser.role) {
        roleId = this.roleToRoleIdMap[apiUser.role] || RoleId.User;
      }
      
      return {
        id: apiUser.id || '',
        email: apiUser.email || '',
        fullName: apiUser.fullName || '',
        phoneNumber: apiUser.phoneNumber || null,
        roleId: roleId,
        isActive: apiUser.isActive !== undefined ? apiUser.isActive : true,
        hasChangedPassword: apiUser.hasChangedPassword !== undefined ? apiUser.hasChangedPassword : true,
        createdAt: apiUser.createdAt || new Date().toISOString(),
        updatedAt: apiUser.updatedAt || null
      };
    } catch (error) {
      console.error('Error mapping user:', apiUser, error);
      return {
        id: apiUser?.id || '',
        email: apiUser?.email || '',
        fullName: apiUser?.fullName || 'Unknown User',
        phoneNumber: apiUser?.phoneNumber || null,
        roleId: RoleId.User,
        isActive: true,
        hasChangedPassword: true,
        createdAt: new Date().toISOString(),
        updatedAt: null
      };
    }
  }

  getRoleLabel(roleId: number): string {
    return ROLE_LABELS[roleId as RoleId] || 'Unknown';
  }

  getTemporaryPassword(userId: string): string | null {
    return this.temporaryPasswords.get(userId) || null;
  }

  hasUserChangedPassword(user: User & { hasChangedPassword?: boolean }): boolean {
    return user.hasChangedPassword !== undefined ? user.hasChangedPassword : true;
  }

  get filteredUsers(): User[] {
    return this.users;
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.pageNumber = page;
      this.loadUsers();
    }
  }

  nextPage(): void {
    if (this.hasNextPage) {
      this.pageNumber++;
      this.loadUsers();
    }
  }

  previousPage(): void {
    if (this.hasPreviousPage) {
      this.pageNumber--;
      this.loadUsers();
    }
  }

  onPageSizeChange(): void {
    this.pageNumber = 1;
    this.loadUsers();
  }

  onFilterChange(): void {
    this.pageNumber = 1;
    this.loadUsers();
  }

  onSearch(): void {
    this.pageNumber = 1;
    this.loadUsers();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedRole = '';
    this.selectedStatus = '';
    this.pageNumber = 1;
    this.loadUsers();
  }

  getPageNumbers(): number[] {
    const pages: number[] = [];
    const maxPagesToShow = 5;
    let startPage = Math.max(1, this.pageNumber - Math.floor(maxPagesToShow / 2));
    let endPage = Math.min(this.totalPages, startPage + maxPagesToShow - 1);
    
    if (endPage - startPage < maxPagesToShow - 1) {
      startPage = Math.max(1, endPage - maxPagesToShow + 1);
    }
    
    for (let i = startPage; i <= endPage; i++) {
      pages.push(i);
    }
    
    return pages;
  }

  Math = Math;

  getAvailableRolesText(): string {
    return this.availableRoles.map(r => r.label).join(', ');
  }

  copyToClipboard(text: string): void {
    navigator.clipboard.writeText(text).then(() => {
      this.successMessage = 'Password copied to clipboard!';
      setTimeout(() => {
        this.successMessage = null;
      }, 2000);
    }).catch(err => {
      console.error('Failed to copy:', err);
      this.error = 'Failed to copy password to clipboard';
      setTimeout(() => {
        this.error = null;
      }, 3000);
    });
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

  openCreateModal(): void {
    this.setupAvailableRoles();
    
    if (this.availableRoles.length === 0) {
      this.error = 'You do not have permission to create users.';
      return;
    }
    
    this.showCreateModal = true;
    this.createUserForm.reset();
    this.error = null;
    this.successMessage = null;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
    this.createUserForm.reset();
    this.error = null;
    this.successMessage = null;
  }

  generateTempPassword(): string {
    const length = 12;
    const lowercase = 'abcdefghijklmnopqrstuvwxyz';
    const uppercase = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
    const numbers = '0123456789';
    const specialChars = '!@#$%^&*';
    const alphanumeric = lowercase + uppercase + numbers;
    const allChars = alphanumeric + specialChars;
    
    let password = '';
    
    password += specialChars.charAt(Math.floor(Math.random() * specialChars.length));
    password += numbers.charAt(Math.floor(Math.random() * numbers.length));
    
    for (let i = 2; i < length; i++) {
      password += allChars.charAt(Math.floor(Math.random() * allChars.length));
    }
    
    return password.split('').sort(() => Math.random() - 0.5).join('');
  }

  onCreateUser(): void {
    if (this.createUserForm.invalid) {
      this.markFormGroupTouched(this.createUserForm);
      return;
    }

    this.submitting = true;
    this.error = null;
    this.successMessage = null;

    const formValue = this.createUserForm.value;
    const tempPassword = this.generateTempPassword();

    const selectedRoleId = typeof formValue.role === 'string' 
      ? parseInt(formValue.role, 10) 
      : formValue.role;
    const selectedRole = this.availableRoles.find(r => r.id === selectedRoleId);
    const roleString = selectedRole?.roleString || 'Driver';

    const createRequest: CreateUserRequest = {
      email: formValue.email,
      password: tempPassword,
      fullName: formValue.fullName,
      role: roleString,
      phoneNumber: formValue.phoneNumber || null
    };

    this.apiService.post<CreateUserResponse>('users', createRequest).subscribe({
      next: (response) => {
        this.submitting = false;
        
        const returnedTempPassword = response.temporaryPassword || tempPassword;
        
        if (returnedTempPassword && (response.hasChangedPassword === false || !response.hasChangedPassword)) {
          this.temporaryPasswords.set(response.id, returnedTempPassword);
        }
        
        this.successMessage = `User created successfully! Temporary password: ${returnedTempPassword}`;
        
        setTimeout(() => {
          this.loadUsers();
          this.closeCreateModal();
        }, 2000);
      },
      error: (err) => {
        this.submitting = false;
        this.error = err.error?.message || err.error?.error || 'Failed to create user. Please try again.';
        console.error('Error creating user:', err);
      }
    });
  }

  confirmRemoveUser(user: User): void {
    if (!this.canRemoveUsers() || this.isCurrentUser(user) || !user.isActive) {
      return;
    }
    this.cancelStatusToggle();
    this.deleteConfirmUserId = user.id;
    this.deleteConfirmExpectedName = user.fullName;
    this.deleteConfirmationInput = '';
    this.error = null;
  }

  cancelRemoveUser(): void {
    this.deleteConfirmUserId = null;
    this.deleteConfirmExpectedName = '';
    this.deleteConfirmationInput = '';
    this.error = null;
  }

  isRemovePhraseValid(): boolean {
    return this.deleteConfirmationInput.trim() === this.deleteConfirmExpectedName.trim();
  }

  executeRemoveUser(): void {
    if (this.deleteConfirmUserId == null || !this.isRemovePhraseValid()) {
      return;
    }
    const id = this.deleteConfirmUserId;
    this.removeSubmitting = true;
    this.error = null;
    this.apiService
      .delete<void>(`users/${id}`)
      .pipe(
        finalize(() => {
          this.removeSubmitting = false;
        })
      )
      .subscribe({
        next: () => {
          this.deleteConfirmUserId = null;
          this.deleteConfirmExpectedName = '';
          this.deleteConfirmationInput = '';
          this.successMessage = 'User deleted successfully.';
          this.loadUsers();
          setTimeout(() => {
            this.successMessage = null;
          }, 4000);
        },
        error: (err) => {
          this.error =
            err.error?.message || err.error?.error || 'Failed to remove user. Please try again.';
          setTimeout(() => {
            this.error = null;
          }, 5000);
        }
      });
  }

  canRemoveUsers(): boolean {
    return this.canEditRoles();
  }

  isCurrentUser(user: User): boolean {
    const current = this.authService.getCurrentUser();
    return !!current && current.id === user.id;
  }

  openStatusToggleConfirm(user: User): void {
    if (this.deleteConfirmUserId !== null) {
      this.cancelRemoveUser();
    }
    this.statusConfirmUser = user;
    this.statusToggleModalError = null;
  }

  cancelStatusToggle(): void {
    this.statusConfirmUser = null;
    this.statusToggleModalError = null;
  }

  executeStatusToggle(): void {
    if (!this.statusConfirmUser) return;
    const user = this.statusConfirmUser;
    const newStatus = !user.isActive;
    const actionWord = newStatus ? 'activated' : 'deactivated';

    this.statusSubmitting = true;
    this.statusToggleModalError = null;

    this.apiService
      .put<UserApiResponse>(`users/${user.id}`, {
        isActive: newStatus
      })
      .pipe(finalize(() => (this.statusSubmitting = false)))
      .subscribe({
        next: (response) => {
          this.statusConfirmUser = null;
          const index = this.users.findIndex(u => u.id === user.id);
          if (index !== -1) {
            this.users[index].isActive = newStatus;
            this.users[index].updatedAt = response.updatedAt || new Date().toISOString();
          }
          this.successMessage = `User ${actionWord} successfully.`;
          setTimeout(() => {
            this.successMessage = null;
          }, 3000);
        },
        error: (err) => {
          this.statusToggleModalError =
            err.error?.message || err.error?.error || `Could not ${newStatus ? 'activate' : 'deactivate'} user.`;
          console.error(`Error updating user status:`, err);
        }
      });
  }

  /** Whether the confirmation modal will activate (vs deactivate) when confirmed. */
  statusToggleWillActivate(): boolean {
    return !!this.statusConfirmUser && !this.statusConfirmUser.isActive;
  }

  canEditRoles(): boolean {
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser) {
      return false;
    }

    const userRoleId = typeof currentUser.roleId === 'string' ? parseInt(currentUser.roleId, 10) : Number(currentUser.roleId);
    return userRoleId === RoleId.SuperAdmin || userRoleId === RoleId.Admin;
  }

  startRoleEdit(user: User): void {
    if (!this.canEditRoles()) {
      return;
    }

    this.editingRoleUserId = user.id;
    this.selectedEditRoleId = user.roleId as RoleId;
    this.error = null;
    this.successMessage = null;
  }

  cancelRoleEdit(): void {
    this.editingRoleUserId = null;
    this.selectedEditRoleId = null;
    this.updatingRole = false;
  }

  saveRoleEdit(user: User): void {
    if (!this.canEditRoles() || this.selectedEditRoleId === null) {
      return;
    }

    const selectedRole = this.availableRoles.find(role => role.id === this.selectedEditRoleId);
    if (!selectedRole) {
      this.error = 'Invalid role selection.';
      return;
    }

    if (selectedRole.id === user.roleId) {
      this.cancelRoleEdit();
      return;
    }

    this.updatingRole = true;
    this.error = null;
    this.successMessage = null;

    this.apiService.put<UserApiResponse>(`users/${user.id}`, { role: selectedRole.roleString }).subscribe({
      next: (response) => {
        const index = this.users.findIndex(u => u.id === user.id);
        if (index !== -1) {
          this.users[index] = this.mapApiResponseToUser(response);
        }

        this.successMessage = `Role updated to ${selectedRole.label} successfully!`;
        this.cancelRoleEdit();
        setTimeout(() => {
          this.successMessage = null;
        }, 3000);
      },
      error: (err) => {
        this.updatingRole = false;
        this.error = err.error?.message || err.error?.error || 'Failed to update role. Please try again.';
        setTimeout(() => {
          this.error = null;
        }, 5000);
      }
    });
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }
}

