import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';

import {
  AlertRule,
  CreateAlertRuleRequest,
  UpdateAlertRuleRequest
} from '../models/alert-rule';

@Injectable({
  providedIn: 'root'
})
export class AlertRuleService {

  private readonly apiUrl =
    `${environment.apiBaseUrl}/api/alert-rules`;

  constructor(
    private readonly http: HttpClient
  ) {}

  getAlertRules(): Observable<AlertRule[]> {
    return this.http.get<AlertRule[]>(this.apiUrl);
  }

  getAlertRule(id: number): Observable<AlertRule> {
    return this.http.get<AlertRule>(`${this.apiUrl}/${id}`);
  }

  createAlertRule(
    request: CreateAlertRuleRequest
  ): Observable<AlertRule> {
    return this.http.post<AlertRule>(
      this.apiUrl,
      request
    );
  }

  updateAlertRule(
    id: number,
    request: UpdateAlertRuleRequest
  ): Observable<AlertRule> {
    return this.http.put<AlertRule>(
      `${this.apiUrl}/${id}`,
      request
    );
  }

  deleteAlertRule(id: number): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/${id}`
    );
  }
}