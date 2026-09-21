import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { AuthService } from '../../../core/services/auth.service';
import { UserRole } from '../../../core/models/user.model';

@Component({
  selector: 'sp-change-password',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent],
  templateUrl: './change-password.component.html',
  styleUrl: './change-password.component.scss',
})
export class ChangePasswordComponent {
  currentPassword = '';
  newPassword = '';
  confirmPassword = '';
  showPasswords = false;
  loading = signal(false);
  error = signal<string | null>(null);
  success = signal(false);

  constructor(public auth: AuthService, private router: Router) {}

  submit(): void {
    this.error.set(null);

    if (!this.currentPassword || !this.newPassword || !this.confirmPassword) {
      this.error.set('Please fill in all fields.');
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.error.set('New password and confirmation do not match.');
      return;
    }

    this.loading.set(true);
    this.auth
      .changePassword({ currentPassword: this.currentPassword, newPassword: this.newPassword })
      .subscribe({
        next: () => {
          this.loading.set(false);
          this.success.set(true);
          setTimeout(() => {
            const role = this.auth.currentUser()?.role;
            this.router.navigate([role ? this.homeFor(role) : '/app']);
          }, 1500);
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(err?.error?.message ?? 'Unable to change password. Please try again.');
        },
      });
  }

  private homeFor(role: UserRole): string {
    switch (role) {
      case 'Generalist':
        return '/app/generalist';
      case 'Specialist':
        return '/app/specialist';
      case 'Admin':
        return '/app/admin';
      case 'TeamLead':
        return '/app/team-lead';
      case 'Polyvalent':
        return '/app/polyvalent';
    }
  }
}
