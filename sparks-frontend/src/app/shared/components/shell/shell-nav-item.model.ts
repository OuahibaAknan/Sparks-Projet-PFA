import { IconName } from '../icon/icon.component';

export interface ShellNavItem {
  label: string;
  icon: IconName;
  route: string;
  /** Match only the exact route, not any route it prefixes (e.g. Dashboard routes like
   * /app/admin, which would otherwise also stay highlighted on /app/admin/users, etc.). */
  exact?: boolean;
}
