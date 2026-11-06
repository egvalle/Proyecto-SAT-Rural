import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { SensorHistory } from '../models/monitoring-history.model';
import { RecentAlert } from '../models/recent-alert.model';

import { DashboardData } from '../models/dashboard.model';
import { AuthService } from '../../../core/services/auth.service';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class MonitoringService {

  private readonly apiUrl = `${environment.apiBaseUrl}/api/monitoring`;

  constructor(
    private http: HttpClient,
    private readonly authService: AuthService
  ) {
  }

  getDashboard(communityId?: number): Observable<DashboardData> {
    return this.http.get<DashboardData>(
      `${this.apiUrl}/dashboard`,
      { params: communityId === undefined ? {} : { communityId } }
    );
  }

  getHistory(
    limit = 20,
    communityId?: number
  ): Observable<SensorHistory[]> {

    return this.http.get<SensorHistory[]>(
      `${this.apiUrl}/history`,
      {
        params: {
          limit,
          ...(communityId === undefined ? {} : { communityId })
        }
      }
    );
  }


  getRecentAlerts(
    limit = 5,
    communityId?: number
  ): Observable<RecentAlert[]> {

    return this.http.get<RecentAlert[]>(
      `${this.apiUrl}/alerts/recent`,
      {
        params: {
          limit,
          ...(communityId === undefined ? {} : { communityId })
        }
      }
    );
  }


  startSimulation(
    scenario: string
  ): Observable<any> {
    if (!this.authService.canWrite()) {
      return this.authService.rejectWrite();
    }

    return this.http.post(
      `${this.apiUrl}/simulation`,
      {
        scenario
      }
    );
  }


  resetSimulation(): Observable<any> {
    if (!this.authService.canWrite()) {
      return this.authService.rejectWrite();
    }

    return this.http.post(
      `${this.apiUrl}/simulation/reset`,
      {}
    );
  }
}
