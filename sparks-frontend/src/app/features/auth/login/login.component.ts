import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { AuthService } from '../../../core/services/auth.service';
import { UserRole } from '../../../core/models/user.model';

@Component({
  selector: 'sp-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, IconComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  email = '';
  password = '';
  showPassword = false;
  loading = signal(false);
  error = signal<string | null>(null);

  constructor(private auth: AuthService, private router: Router) {}

  submit(): void {
    if (!this.email || !this.password) {
      this.error.set('Please enter your email and password.');
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    this.auth.login({ email: this.email, password: this.password }).subscribe({
      next: (res) => {
        this.loading.set(false);
        this.router.navigate([this.homeFor(res.user.role)]);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(
          err?.status === 0
            ? 'The server is unavailable. Please try again later.'
            : (err?.error?.message ?? 'Invalid email or password.'),
        );
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
