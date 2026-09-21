import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs/operators';
import { IconComponent } from '../icon/icon.component';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { NAV_ITEMS_BY_ROLE } from '../../constants/nav-items';
import { ShellNavItem } from './shell-nav-item.model';
import { toAssetUrl } from '../../utils/asset-url';

export type { ShellNavItem } from './shell-nav-item.model';

@Component({
  selector: 'sp-shell',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet, IconComponent],
  template: `
    <div class="shell">
      <aside class="shell__sidebar">
        <div class="shell__brand">
          <img src="/logo-sparks-full.png" alt="SPARKS" class="shell__brand-logo" />
          <div class="shell__brand-sub">PLM Support Platform</div>
        </div>

        <nav class="shell__nav">
          @for (item of navItems(); track item.route) {
            <a
              class="shell__nav-item"
              [routerLink]="item.route"
              routerLinkActive="shell__nav-item--active"
              [routerLinkActiveOptions]="{ exact: !!item.exact }"
            >
              <sp-icon [name]="item.icon" [size]="18"></sp-icon>
              {{ item.label }}
            </a>
          }
        </nav>

        <a
          class="shell__security-alert"
          routerLink="/app/change-password"
          *ngIf="auth.currentUser()?.mustChangePassword"
        >
          <sp-icon name="alert-triangle" [size]="16"></sp-icon>
          <span>Change your password now for security.</span>
        </a>
      </aside>

      <div class="shell__main">
        <header class="shell__topbar">
          <div class="shell__topbar-right">
            <button
              *ngIf="showAvailability()"
              class="shell__availability"
              [class.shell__availability--away]="!available"
              (click)="available = !available"
            >
              <span class="shell__availability-dot"></span>
              <span class="shell__availability-text">{{ available ? 'Available' : 'Away' }}</span>
              <span class="shell__availability-role" *ngIf="currentUserRole() as role">{{ role }}</span>
            </button>
            <button class="shell__icon-btn" title="Notifications" routerLink="/app/notifications">
              <sp-icon name="bell" [size]="18"></sp-icon>
              <span class="shell__badge" *ngIf="unreadCount > 0">{{ unreadCount > 9 ? '9+' : unreadCount }}</span>
            </button>
            <div class="shell__user-menu-wrap" *ngIf="auth.currentUser() as user">
              <button
                class="shell__avatar shell__avatar--sm shell__avatar--btn"
                [style.background-image]="photoUrl(user) ? 'url(' + photoUrl(user) + ')' : null"
                (click)="userMenuOpen.set(!userMenuOpen())"
                title="Account menu"
              >
                <span *ngIf="!photoUrl(user)">{{ user.initials }}</span>
              </button>

              <div class="shell__user-menu-backdrop" *ngIf="userMenuOpen()" (click)="userMenuOpen.set(false)"></div>

              <div class="shell__user-menu" *ngIf="userMenuOpen()">
                <a class="shell__user-menu-item" routerLink="/app/profile" (click)="userMenuOpen.set(false)">
                  <sp-icon name="user" [size]="15"></sp-icon>
                  Profile
                </a>
                <button class="shell__user-menu-item shell__user-menu-item--danger" (click)="logout()">
                  <sp-icon name="log-out" [size]="15"></sp-icon>
                  Se déconnecter
                </button>
              </div>
            </div>
          </div>
        </header>
        <main class="shell__content">
          <router-outlet></router-outlet>
        </main>
      </div>
    </div>
  `,
  styleUrls: ['./shell.component.scss'],
})
export class ShellComponent implements OnInit {
  available = true;
  unreadCount = 0;
  userMenuOpen = signal(false);

  constructor(public auth: AuthService, private router: Router, private notifications: NotificationService) {}

  ngOnInit(): void {
    this.refreshUnreadCount();
    this.router.events.pipe(filter((e) => e instanceof NavigationEnd)).subscribe(() => this.refreshUnreadCount());
  }

  private refreshUnreadCount(): void {
    this.notifications.unreadCount().subscribe((count) => (this.unreadCount = count));
  }

  photoUrl(user: { profilePhotoUrl: string | null }): string | null {
    return toAssetUrl(user.profilePhotoUrl);
  }

  navItems(): ShellNavItem[] {
    const role = this.auth.currentUser()?.role;
    return role ? NAV_ITEMS_BY_ROLE[role] : [];
  }

  showAvailability(): boolean {
    return !!this.auth.currentUser()?.role;
  }

  currentUserRole(): string | null {
    const role = this.auth.currentUser()?.role;

    if (!role) {
      return null;
    }

    if (role === 'TeamLead') {
      return 'Team Lead';
    }

    return role;
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
