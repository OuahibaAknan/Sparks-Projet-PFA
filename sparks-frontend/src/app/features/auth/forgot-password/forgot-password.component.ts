import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { AuthService } from '../../../core/services/auth.service';

type Step = 'email' | 'reset';

@Component({
  selector: 'sp-forgot-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, IconComponent],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss',
})
export class ForgotPasswordComponent {
  step = signal<Step>('email');
  loading = signal(false);
  error = signal<string | null>(null);
  success = signal(false);

  email = '';
  code = '';
  newPassword = '';
  confirmPassword = '';

  constructor(private auth: AuthService, private router: Router) {}

  submitEmail(): void {
    this.error.set(null);
    if (!this.email.trim()) {
      this.error.set('Please enter your email.');
      return;
    }

    this.loading.set(true);
    this.auth.forgotPassword({ email: this.email.trim() }).subscribe({
      next: () => {
        this.loading.set(false);
        this.step.set('reset');
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message ?? 'Unable to send the reset code. Please try again.');
      },
    });
  }

  submitReset(): void {
    this.error.set(null);

    if (!this.code.trim() || !this.newPassword || !this.confirmPassword) {
      this.error.set('Please fill in all fields.');
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.error.set('New password and confirmation do not match.');
      return;
    }

    this.loading.set(true);
    this.auth
      .resetPasswordWithCode({ email: this.email.trim(), code: this.code.trim(), newPassword: this.newPassword })
      .subscribe({
        next: () => {
          this.loading.set(false);
          this.success.set(true);
          setTimeout(() => this.router.navigate(['/login']), 1800);
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(err?.error?.message ?? 'Unable to reset your password. Please try again.');
        },
      });
  }
}
