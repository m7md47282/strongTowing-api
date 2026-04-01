import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../../../../services/auth.service';
import { SettingsService, DispatchContact } from '../../../../services/settings.service';
import { User } from '../../../../models/user.model';
import { parseApiError } from '../../../../utils/api-error.util';

@Component({
  selector: 'app-driver-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './driver-profile.component.html',
  styleUrl: './driver-profile.component.scss'
})
export class DriverProfileComponent implements OnInit {
  user: User | null = null;
  passwordForm: FormGroup;
  passwordMessage: string | null = null;
  passwordError: string | null = null;
  passwordSubmitting = false;

  dispatchContact: DispatchContact | null = null;
  availabilitySubmitting = false;
  availabilityError: string | null = null;

  constructor(
    private auth: AuthService,
    private settings: SettingsService,
    private fb: FormBuilder
  ) {
    this.passwordForm = this.fb.group({
      currentPassword: ['', [Validators.required]],
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]]
    });
  }

  ngOnInit(): void {
    this.user = this.auth.getCurrentUser();
    this.settings.getDispatchContact().subscribe({
      next: (c) => (this.dispatchContact = c),
      error: () => (this.dispatchContact = null)
    });
  }

  get onDuty(): boolean {
    return this.user?.isAvailableForDispatch !== false;
  }

  setAvailability(available: boolean): void {
    this.availabilitySubmitting = true;
    this.availabilityError = null;
    this.auth
      .updateDriverAvailability(available)
      .pipe(finalize(() => (this.availabilitySubmitting = false)))
      .subscribe({
        next: (u) => (this.user = u),
        error: (err) => {
          this.availabilityError = parseApiError(err);
        }
      });
  }

  onChangePassword(): void {
    this.passwordMessage = null;
    this.passwordError = null;

    const newPw = this.passwordForm.value.newPassword as string;
    const confirm = this.passwordForm.value.confirmPassword as string;
    if (newPw !== confirm) {
      this.passwordError = 'New password and confirmation do not match.';
      return;
    }

    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.passwordSubmitting = true;
    this.auth
      .changePassword(
        this.passwordForm.value.currentPassword as string,
        newPw
      )
      .pipe(finalize(() => (this.passwordSubmitting = false)))
      .subscribe({
        next: () => {
          this.passwordMessage = 'Password updated successfully.';
          this.passwordForm.reset();
          this.user = this.auth.getCurrentUser();
        },
        error: (err) => {
          this.passwordError = parseApiError(err);
        }
      });
  }

  isFieldInvalid(name: string): boolean {
    const c = this.passwordForm.get(name);
    return !!(c && c.invalid && (c.dirty || c.touched));
  }

  telHref(phone: string | null | undefined): string {
    const digits = (phone || '').replace(/\D/g, '');
    return digits ? `tel:${digits}` : '';
  }
}
