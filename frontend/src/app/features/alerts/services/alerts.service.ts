import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { Alert } from '../models/alert.model';
import { environment } from '../../../../environments/environment';

export interface AlertFilters {
  level?: string;
  status?: string;
  communityId?: number;
  sensorId?: number;
  phenomenon?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({
  providedIn: 'root'
})
export class AlertsService {

  private readonly apiUrl = `${environment.apiBaseUrl}/api/alerts`;

  constructor(private http: HttpClient) {
  }

  getAlerts(filters: AlertFilters = {}): Observable<Alert[]> {

    const params: Record<string, string> = {};

    for (const [key, value] of Object.entries(filters)) {
      if (value !== undefined && value !== '') {
        params[key] = String(value);
      }
    }

    return this.http.get<Alert[]>(this.apiUrl, { params });
  }

  getAlert(id: number): Observable<Alert> {
    return this.http.get<Alert>(`${this.apiUrl}/${id}`);
  }

  attendAlert(id: number): Observable<{ message: string }> {
    return this.http.patch<{ message: string }>(`${this.apiUrl}/${id}/attend`, {});
  }

  resolveAlert(id: number): Observable<{ message: string }> {
    return this.http.patch<{ message: string }>(
      `${this.apiUrl}/${id}/resolve`,
      {}
    );
  }
}
