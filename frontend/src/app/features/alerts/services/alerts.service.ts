import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { Alert } from '../models/alert.model';

@Injectable({
 providedIn: 'root'
})
export class AlertsService {

 private readonly apiUrl = '/api/alerts';

 constructor(private http: HttpClient) {
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
 return this.http.patch<{ message: string }>(
 `${this.apiUrl}/${id}/resolve`,
 {}
 );
 }
}
