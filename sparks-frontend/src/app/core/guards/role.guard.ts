import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { UserRole } from '../models/user.model';

export const roleGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const allowedRoles = route.data['roles'] as UserRole[] | undefined;
  const user = auth.currentUser();

  if (!user) return router.createUrlTree(['/login']);
  if (!allowedRoles || allowedRoles.includes(user.role)) return true;

  const home: Record<UserRole, string> = {
    Generalist: '/app/generalist',
    Specialist: '/app/specialist',
    Admin: '/app/admin',
    TeamLead: '/app/team-lead',
    Polyvalent: '/app/polyvalent',
  };
  return router.createUrlTree([home[user.role]]);
};
