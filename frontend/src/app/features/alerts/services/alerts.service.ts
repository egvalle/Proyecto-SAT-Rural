import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { AuthService } from '../../../core/services/auth.service';
import { Alert } from '../models/alert.model';

@Injectable({
 providedIn: 'root'
})
export class AlertsService {

 private readonly apiUrl = '/api/alerts';

 constructor(
  private http: HttpClient,
  private authService: AuthService
 ) {
 }

 getAlerts(
 level?: string,
 isActive?: boolean
 ): Observable<Alert[]> {

 const params: Record<string, string> = {};

 if (level) {
 params['level'] = level;
 }

 if (isActive !== undefined) {
 params['isActive'] = String(isActive);
 }

 return this.http.get<Alert[]>(this.apiUrl, { params });
 }

 resolveAlert(id: number): Observable<{ message: string }> {
 if (!this.authService.canWrite()) {
 return this.authService.rejectWrite();
 }

 return this.http.patch<{ message: string }>(
 `${this.apiUrl}/${id}/resolve`,
 {}
 );
 }
}
