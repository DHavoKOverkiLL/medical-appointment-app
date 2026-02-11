import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, UrlTree } from '@angular/router';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class RoleGuard implements CanActivate {
  constructor(private auth: AuthService, private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot): boolean | UrlTree {
    if (!this.auth.isLoggedIn()) {
      return this.router.createUrlTree(['/login']);
    }

    const allowedRoles = route.data['roles'] as string[] | undefined;
    if (!allowedRoles || allowedRoles.length === 0) {
      return true;
    }

    const currentRole = this.auth.getUserRoleNormalized();
    const normalizedAllowedRoles = allowedRoles.map(role => role.trim().toLowerCase());
    if (currentRole && normalizedAllowedRoles.includes(currentRole)) {
      return true;
    }

    return this.router.createUrlTree(['/dashboard']);
  }
}
