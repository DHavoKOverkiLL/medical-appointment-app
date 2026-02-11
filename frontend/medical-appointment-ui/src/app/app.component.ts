import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { AuthService } from './auth/auth.service';
import { HeaderComponent } from './layout/header/header.component';
import { AppLanguage, I18nService } from './core/i18n/i18n.service';
import { LoadingService } from './core/loading/loading.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterModule, CommonModule, HeaderComponent, TranslateModule, MatProgressBarModule],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  constructor(
    public authService: AuthService,
    public i18n: I18nService,
    public readonly loadingService: LoadingService,
    private router: Router
  ) {}

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  setLanguage(language: AppLanguage): void {
    this.i18n.setLanguage(language);
  }
}
