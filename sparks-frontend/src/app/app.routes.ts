import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { ShellComponent } from './shared/components/shell/shell.component';

const ALL_ROLES = ['Generalist', 'Specialist', 'Admin', 'TeamLead', 'Polyvalent'];

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/landing/landing.component').then((m) => m.LandingComponent),
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./features/auth/forgot-password/forgot-password.component').then((m) => m.ForgotPasswordComponent),
  },
  {
    path: 'app',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      {
        path: 'generalist',
        canActivate: [roleGuard],
        data: { roles: ['Generalist'] },
        loadComponent: () =>
          import('./features/generalist/dashboard/dashboard.component').then((m) => m.GeneralistDashboardComponent),
      },
      {
        path: 'specialist',
        canActivate: [roleGuard],
        data: { roles: ['Specialist'] },
        loadComponent: () =>
          import('./features/specialist/dashboard/dashboard.component').then((m) => m.SpecialistDashboardComponent),
      },
      {
        path: 'admin',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () => import('./features/admin/dashboard/dashboard.component').then((m) => m.AdminDashboardComponent),
      },
      {
        path: 'team-lead',
        canActivate: [roleGuard],
        data: { roles: ['TeamLead'] },
        loadComponent: () =>
          import('./features/team-lead/dashboard/dashboard.component').then((m) => m.TeamLeadDashboardComponent),
      },
      {
        path: 'polyvalent',
        canActivate: [roleGuard],
        data: { roles: ['Polyvalent'] },
        loadComponent: () =>
          import('./features/polyvalent/dashboard/dashboard.component').then((m) => m.PolyvalentDashboardComponent),
      },
      {
        path: 'admin/users',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () =>
          import('./features/admin/user-management/user-management.component').then((m) => m.UserManagementComponent),
      },
      {
        path: 'admin/tickets',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'TeamLead'] },
        loadComponent: () => import('./features/tickets/ticket-list/ticket-list.component').then((m) => m.TicketListComponent),
      },
      {
        path: 'admin/categories-sla',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () =>
          import('./features/admin/categories-sla/categories-sla.component').then((m) => m.CategoriesSlaComponent),
      },
      {
        path: 'admin/ai-insights',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () => import('./features/admin/ai-insights/ai-insights.component').then((m) => m.AiInsightsComponent),
      },
      {
        path: 'admin/reports',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () => import('./features/admin/reports/reports.component').then((m) => m.ReportsComponent),
      },
      {
        path: 'admin/settings',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () => import('./features/admin/settings/settings.component').then((m) => m.SettingsComponent),
      },
      {
        path: 'tickets',
        canActivate: [roleGuard],
        data: { roles: ['Generalist', 'Specialist', 'Polyvalent'] },
        loadComponent: () =>
          import('./features/tickets/ticket-management/ticket-management.component').then(
            (m) => m.TicketManagementComponent
          ),
      },
      {
        path: 'tickets/:id',
        canActivate: [roleGuard],
        data: { roles: ALL_ROLES },
        loadComponent: () => import('./features/tickets/ticket-detail/ticket-detail.component').then((m) => m.TicketDetailComponent),
      },
      {
        path: 'notifications',
        canActivate: [roleGuard],
        data: { roles: ALL_ROLES },
        loadComponent: () =>
          import('./features/account/notifications/notifications.component').then((m) => m.NotificationsComponent),
      },
      {
        path: 'profile',
        canActivate: [roleGuard],
        data: { roles: ALL_ROLES },
        loadComponent: () => import('./features/account/profile/profile.component').then((m) => m.ProfileComponent),
      },
      {
        path: 'change-password',
        canActivate: [roleGuard],
        data: { roles: ALL_ROLES },
        loadComponent: () =>
          import('./features/account/change-password/change-password.component').then(
            (m) => m.ChangePasswordComponent
          ),
      },
      { path: '', pathMatch: 'full', redirectTo: 'generalist' },
    ],
  },
  { path: '**', redirectTo: '' },
];
