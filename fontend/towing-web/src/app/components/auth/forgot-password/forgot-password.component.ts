import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../services/auth.service';

const PASSWORD_PATTERN = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/;

@Component({
  selector: 'app-forgot-password',
  imports: [ReactiveFormsModule, RouterModule],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss'
})
export class ForgotPasswordComponent {
  step: 'email' | 'reset' = 'email';
  emailForm: FormGroup;
  resetForm: FormGroup;
  pendingEmail = '';
  isLoading = false;
  isResending = false;
  errorMessage = '';
  successMessage = '';
  showPassword = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.emailForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });
    this.resetForm = this.fb.group(
      {
        otp: ['', [Validators.required, Validators.minLength(4), Validators.maxLength(10)]],
        newPassword: ['', [Validators.required, Validators.minLength(8), Validators.pattern(PASSWORD_PATTERN)]],
        confirmPassword: ['', [Validators.required]]
      },
      { validators: this.passwordMatchValidator }
    );
  }

  passwordMatchValidator(control: AbstractControl): { mismatch?: boolean } | null {
    const np = control.get('newPassword');
    const cp = control.get('confirmPassword');
    if (np && cp && np.value !== cp.value) {
      return { mismatch: true };
    }
    return null;
  }

  submitEmail(): void {
    if (this.emailForm.invalid) {
      this.emailForm.markAllAsTouched();
      return;
    }
    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';
    const email = String(this.emailForm.value.email).trim();
    this.authService.forgotPassword({ email }).subscribe({
      next: (r) => {
        this.isLoading = false;
        this.pendingEmail = email;
        this.successMessage = r.message ?? 'Check your email for a reset code.';
        this.step = 'reset';
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message || 'Could not send reset email. Try again later.';
      }
    });
  }

  submitReset(): void {
    if (this.resetForm.invalid) {
      this.resetForm.markAllAsTouched();
      return;
    }
    this.isLoading = true;
    this.errorMessage = '';
    const v = this.resetForm.getRawValue();
    this.authService
      .resetPassword({
        email: this.pendingEmail,
        otp: String(v.otp).trim(),
        newPassword: v.newPassword
      })
      .subscribe({
        next: (r) => {
          this.isLoading = false;
          this.successMessage = r.message ?? 'Password updated.';
          setTimeout(() => this.router.navigate(['/login']), 1500);
        },
        error: (err) => {
          this.isLoading = false;
          this.errorMessage =
            err.error?.message || err.error?.error || 'Could not reset password. Check the code and try again.';
        }
      });
  }

  resendCode(): void {
    if (!this.pendingEmail) {
      return;
    }
    this.isResending = true;
    this.authService.resendOtp(this.pendingEmail, 'passwordReset').subscribe({
      next: (r) => {
        this.isResending = false;
        this.successMessage = r.message ?? 'If an account exists, a new code was sent.';
      },
      error: () => {
        this.isResending = false;
        this.errorMessage = 'Could not resend the code.';
      }
    });
  }

  backToEmail(): void {
    this.step = 'email';
    this.errorMessage = '';
    this.successMessage = '';
  }

  togglePw(): void {
    this.showPassword = !this.showPassword;
  }

  isInvalid(form: FormGroup, field: string): boolean {
    const c = form.get(field);
    return !!(c && c.invalid && (c.dirty || c.touched));
  }
}
