import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { RegisterRequest } from '../../../models/user.model';
import { USER_ROLES, RoleId } from '../../../constants/user-roles.constants';

/** Matches API SignupRequest password rules */
const PASSWORD_PATTERN = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/;

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterModule],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent implements OnInit {
  registerForm: FormGroup;
  otpForm: FormGroup;
  isLoading = false;
  isResending = false;
  errorMessage = '';
  successMessage = '';
  showPassword = false;
  signupStep: 'form' | 'otp' = 'form';
  pendingEmail = '';

  readonly roles = USER_ROLES;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.registerForm = this.fb.group({
      fullName: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: [''],
      password: ['', [Validators.required, Validators.minLength(8), Validators.pattern(PASSWORD_PATTERN)]],
      confirmPassword: ['', [Validators.required]],
      roleId: [RoleId.Driver, [Validators.required]],
      agreeToTerms: [false, [Validators.requiredTrue]]
    }, { validators: this.passwordMatchValidator });

    this.otpForm = this.fb.group({
      email: [{ value: '', disabled: true }],
      otp: ['', [Validators.required, Validators.minLength(4), Validators.maxLength(10)]]
    });
  }

  ngOnInit(): void {
    if (this.authService.isAuthenticated()) {
      this.router.navigate(['/dashboard']);
    }
  }

  passwordMatchValidator(control: AbstractControl): { [key: string]: boolean } | null {
    const password = control.get('password');
    const confirmPassword = control.get('confirmPassword');
    if (password && confirmPassword && password.value !== confirmPassword.value) {
      return { mismatch: true };
    }
    return null;
  }

  onSubmitRegister(): void {
    if (this.registerForm.invalid) {
      this.markFormGroupTouched(this.registerForm);
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    const formValue = this.registerForm.getRawValue();
    const registerRequest: RegisterRequest = {
      email: formValue.email,
      password: formValue.password,
      fullName: formValue.fullName,
      roleId: formValue.roleId,
      phoneNumber: formValue.phoneNumber || null
    };

    this.authService.register(registerRequest).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.requiresVerification) {
          this.pendingEmail = res.email ?? formValue.email;
          this.signupStep = 'otp';
          this.otpForm.patchValue({ email: this.pendingEmail, otp: '' });
          this.successMessage =
            res.message ?? 'We sent a verification code to your email. Enter it below to finish signing up.';
        } else {
          this.successMessage = 'Registration submitted.';
        }
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage =
          error.error?.message ||
          error.error?.error ||
          'Registration failed. Please try again.';
      }
    });
  }

  onSubmitOtp(): void {
    if (this.otpForm.invalid) {
      this.markFormGroupTouched(this.otpForm);
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.authService
      .verifySignupOtp({
        email: this.pendingEmail,
        otp: String(this.otpForm.getRawValue().otp).trim()
      })
      .subscribe({
        next: () => {
          this.isLoading = false;
          this.redirectAfterLogin();
        },
        error: (error) => {
          this.isLoading = false;
          this.errorMessage =
            error.error?.message ||
            error.error?.error ||
            'Invalid or expired code. Try again or request a new one.';
        }
      });
  }

  resendOtp(): void {
    if (!this.pendingEmail) {
      return;
    }
    this.isResending = true;
    this.errorMessage = '';
    this.authService.resendOtp(this.pendingEmail, 'signup').subscribe({
      next: (r) => {
        this.isResending = false;
        this.successMessage = r.message ?? 'A new code was sent.';
      },
      error: () => {
        this.isResending = false;
        this.errorMessage = 'Could not resend the code. Try again later.';
      }
    });
  }

  backToForm(): void {
    this.signupStep = 'form';
    this.errorMessage = '';
    this.successMessage = '';
  }

  private redirectAfterLogin(): void {
    const user = this.authService.getCurrentUser();
    if (!user) {
      this.router.navigate(['/login']);
      return;
    }
    const roleId = typeof user.roleId === 'string' ? parseInt(user.roleId, 10) : Number(user.roleId);
    if (roleId === RoleId.SuperAdmin || roleId === RoleId.Admin) {
      this.router.navigate(['/admin']);
    } else if (roleId === RoleId.Dispatcher) {
      this.router.navigate(['/dispatcher']);
    } else if (roleId === RoleId.Driver) {
      this.router.navigate(['/driver']);
    } else {
      this.router.navigate(['/customer']);
    }
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  isFieldInvalid(form: FormGroup, fieldName: string): boolean {
    const field = form.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  private markFormGroupTouched(form: FormGroup): void {
    Object.keys(form.controls).forEach((key) => {
      form.get(key)?.markAsTouched();
    });
  }
}
