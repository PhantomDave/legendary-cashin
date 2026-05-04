// noinspection ExceptionCaughtLocallyJS

import {inject, Injectable} from '@angular/core';
import {HttpClient, HttpHeaders} from '@angular/common/http';
import {firstValueFrom} from 'rxjs';
import {CookieService} from 'ngx-cookie-service';


export interface ApiRequestOptions {
  headers?: Record<string, string> | HttpHeaders;
  params?: Record<string, string | number | boolean>;
  observe?: 'body' | 'events' | 'response';
  reportProgress?: boolean;
  responseType?: 'json' | 'arraybuffer' | 'blob' | 'text';
  withCredentials?: boolean;
  body?: unknown;
}

@Injectable({
  providedIn: 'root',
})
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly cookieService = inject(CookieService);

  get<T>(url: string, options?: ApiRequestOptions, withCredentials?: boolean): Promise<T> {
    return this.request<T>('GET', url, undefined, options, withCredentials);
  }

  post<T>(url: string, body: unknown, options?: ApiRequestOptions, withCredentials?: boolean): Promise<T> {
    return this.request<T>('POST', url, body, options, withCredentials);
  }

  put<T>(url: string, body: unknown, options?: ApiRequestOptions, withCredentials?: boolean): Promise<T> {
    return this.request<T>('PUT', url, body, options, withCredentials);
  }

  delete<T>(url: string, options?: ApiRequestOptions, withCredentials?: boolean): Promise<T> {
    return this.request<T>('DELETE', url, undefined, options, withCredentials);
  }

  patch<T>(url: string, body: unknown, options?: ApiRequestOptions, withCredentials?: boolean): Promise<T> {
    return this.request<T>('PATCH', url, body, options, withCredentials);
  }

  async request<T>(method: string, url: string, body?: unknown, options?: ApiRequestOptions, withCredentials?: boolean): Promise<T> {

      method = method.toUpperCase();

      if (method === 'GET' || method === 'DELETE') {
        if (body) {
          throw new Error('GET and DELETE requests cannot have a body');
        }
      } else {
        if (!body) {
          throw new Error('POST and PUT requests must have a body');
        }
      }

      if (!options) {
        options = {};
      }

      let headers: HttpHeaders;
      if (options.headers instanceof HttpHeaders) {
        headers = options.headers;
      } else if (options.headers) {
        headers = new HttpHeaders(options.headers);
      } else {
        headers = new HttpHeaders();
      }

      if (!headers.has('Content-Type') && !(body instanceof FormData)) {
        headers = headers.set('Content-Type', 'application/json');
      }

      const jwt = this.cookieService.get('jwt_session');

      if (withCredentials) {
        if (!jwt) {
          throw new Error('Unauthorized');
        }
        headers = headers.set('Authorization', `Bearer ${jwt}`);
        options.withCredentials = true;
      }

      const httpOptions: Record<string, unknown> = {
        headers,
        body,
      };

      if (options.params) {
        httpOptions['params'] = options.params;
      }
      if (options.reportProgress !== undefined) {
        httpOptions['reportProgress'] = options.reportProgress;
      }
      if (options.withCredentials !== undefined) {
        httpOptions['withCredentials'] = options.withCredentials;
      }

      // Converts the Observable to a Promise.
      // HttpClient automatically throws HttpErrorResponse for non-2xx — callers handle it with try/catch.
      return firstValueFrom(this.http.request<T>(method, url, httpOptions));
    }
}
