import { registerLocaleData } from '@angular/common';
import { bootstrapApplication } from '@angular/platform-browser';
import localeEnGb from '@angular/common/locales/en-GB';
import localeRo from '@angular/common/locales/ro';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';

registerLocaleData(localeEnGb);
registerLocaleData(localeRo);

bootstrapApplication(AppComponent, appConfig)
  .catch((err) => console.error(err));
