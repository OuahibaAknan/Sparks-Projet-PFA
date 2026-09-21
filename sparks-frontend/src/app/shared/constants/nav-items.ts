import { UserRole } from '../../core/models/user.model';
import { ShellNavItem } from '../components/shell/shell-nav-item.model';

export const NAV_ITEMS_BY_ROLE: Record<UserRole, ShellNavItem[]> = {
  Generalist: [
    { label: 'Dashboard', icon: 'grid', route: '/app/generalist', exact: true },
    { label: 'Ticket Management', icon: 'ticket', route: '/app/tickets' },
    { label: 'Notifications', icon: 'bell', route: '/app/notifications' },
    { label: 'Profile', icon: 'user', route: '/app/profile' },
  ],
  Specialist: [
    { label: 'Dashboard', icon: 'grid', route: '/app/specialist', exact: true },
    { label: 'Ticket Management', icon: 'ticket', route: '/app/tickets' },
    { label: 'Notifications', icon: 'bell', route: '/app/notifications' },
    { label: 'Profile', icon: 'user', route: '/app/profile' },
  ],
  Admin: [
    { label: 'Dashboard', icon: 'grid', route: '/app/admin', exact: true },
    { label: 'User Management', icon: 'users', route: '/app/admin/users' },
    { label: 'Ticket Monitoring', icon: 'monitor', route: '/app/admin/tickets' },
    { label: 'Categories & SLA', icon: 'tag', route: '/app/admin/categories-sla' },
    { label: 'AI Insights', icon: 'sparkles', route: '/app/admin/ai-insights' },
    { label: 'Reports', icon: 'bar-chart', route: '/app/admin/reports' },
    { label: 'Settings', icon: 'settings', route: '/app/admin/settings' },
    { label: 'Profile', icon: 'user', route: '/app/profile' },
  ],
  TeamLead: [
    { label: 'Dashboard', icon: 'grid', route: '/app/team-lead', exact: true },
    { label: 'Ticket Monitoring', icon: 'monitor', route: '/app/admin/tickets' },
    { label: 'Notifications', icon: 'bell', route: '/app/notifications' },
    { label: 'Profile', icon: 'user', route: '/app/profile' },
  ],
  Polyvalent: [
    { label: 'Dashboard', icon: 'grid', route: '/app/polyvalent', exact: true },
    { label: 'Ticket Management', icon: 'ticket', route: '/app/tickets' },
    { label: 'Notifications', icon: 'bell', route: '/app/notifications' },
    { label: 'Profile', icon: 'user', route: '/app/profile' },
  ],
};
