import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { Permissao } from '../models/auth.models';

export function permissionGuard(permissao: Permissao): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (auth.temPermissao(permissao)) return true;

    return router.createUrlTree(['/']);
  };
}
