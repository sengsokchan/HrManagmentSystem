import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ApiService {
  constructor(private readonly http: HttpClient) {}

  async request<T>(
    path: string,
    method = 'GET',
    body?: unknown,
    query?: Record<string, string | number | boolean | null | undefined>
  ): Promise<T> {
    let headers = new HttpHeaders({ 'Content-Type': 'application/json' });
    let params = new HttpParams();

    if (query) {
      for (const [key, value] of Object.entries(query)) {
        if (value !== null && value !== undefined && value !== '') {
          params = params.set(key, String(value));
        }
      }
    }

    return firstValueFrom(
      this.http.request<T>(method, path, {
        body,
        headers,
        params
      })
    );
  }
}
