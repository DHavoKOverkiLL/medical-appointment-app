import { Injectable } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export type AppLanguage = 'ro' | 'en';

@Injectable({
  providedIn: 'root'
})
export class I18nService {
  private readonly storageKey = 'app.language';
  readonly defaultLanguage: AppLanguage = 'ro';
  readonly supportedLanguages: AppLanguage[] = ['ro', 'en'];

  constructor(private readonly translate: TranslateService) {}

  init(): void {
    this.translate.addLangs(this.supportedLanguages);
    this.translate.setDefaultLang(this.defaultLanguage);

    const storedLanguage = this.getStoredLanguage();
    const browserLanguage = this.toSupportedLanguage(this.translate.getBrowserLang());
    const initialLanguage = storedLanguage ?? browserLanguage ?? this.defaultLanguage;
    this.useLanguage(initialLanguage);
  }

  setLanguage(language: AppLanguage): void {
    this.useLanguage(language);
  }

  get currentLanguage(): AppLanguage {
    return this.toSupportedLanguage(this.translate.currentLang) ?? this.defaultLanguage;
  }

  private useLanguage(language: AppLanguage): void {
    this.translate.use(language);

    if (typeof localStorage !== 'undefined') {
      localStorage.setItem(this.storageKey, language);
    }

    if (typeof document !== 'undefined') {
      document.documentElement.lang = language;
    }
  }

  private getStoredLanguage(): AppLanguage | null {
    if (typeof localStorage === 'undefined') {
      return null;
    }

    const value = localStorage.getItem(this.storageKey);
    return this.toSupportedLanguage(value);
  }

  private toSupportedLanguage(value: string | null | undefined): AppLanguage | null {
    if (!value) {
      return null;
    }

    const normalized = value.toLowerCase().trim();
    if (normalized === 'ro' || normalized === 'en') {
      return normalized;
    }

    return null;
  }
}
