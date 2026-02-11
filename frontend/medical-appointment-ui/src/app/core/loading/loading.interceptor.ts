import { Injectable } from '@angular/core';
import {
  HttpContextToken,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { API_BASE_URL } from '../api.config';
import { LoadingService } from './loading.service';

export const SKIP_GLOBAL_LOADING = new HttpContextToken<boolean>(() => false);

@Injectable()
export class LoadingInterceptor implements HttpInterceptor {
  constructor(private readonly loadingService: LoadingService) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const shouldTrackRequest = req.url.startsWith(API_BASE_URL) && !req.context.get(SKIP_GLOBAL_LOADING);
    if (!shouldTrackRequest) {
      return next.handle(req);
    }

    this.loadingService.begin();

    return next.handle(req).pipe(
      finalize(() => {
        this.loadingService.end();
      })
    );
  }
}
