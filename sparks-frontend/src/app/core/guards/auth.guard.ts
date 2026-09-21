import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';


// pose une seule question binaire :
//  "es-tu connectée, oui ou non ?"
//  Si oui → accès autorisé.
//  Si non → redirection vers le login.
//  Il ne vérifie pas du tout le rôle
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated()) return true;
  return router.createUrlTree(['/login']);
};
