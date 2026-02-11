import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../auth/auth.service';

import { Router, RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';


@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterModule, TranslateModule],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})

export class HomeComponent implements OnInit {
  role = '';
  normalizedRole = '';
  constructor(public authService: AuthService, private router: Router) {}

  ngOnInit(): void {
    this.role = this.authService.getUserRole() || '';
    this.normalizedRole = this.authService.getUserRoleNormalized() || '';

    if (!this.normalizedRole) {
      this.authService.logout();
      this.router.navigate(['/login']);
      return;
    }

    if (this.normalizedRole === 'admin') {
      this.router.navigate(['/dashboard/admin']);
      return;
    }

    if (this.normalizedRole === 'doctor') {
      this.router.navigate(['/dashboard/doctor']);
      return;
    }

    if (this.normalizedRole === 'patient') {
      this.router.navigate(['/dashboard/patient']);
    }
  }
}
