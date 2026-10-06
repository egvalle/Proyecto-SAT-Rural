import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  SensorReading,
  SensorReadingPagedResult
} from '../models/sensor-reading';

@Injectable({
  providedIn: 'root'
})
export class SensorReadingService {

  private readonly readingsUrl =
    `${environment.apiBaseUrl}/api/sensor-readings`;

  constructor(
    private readonly http: HttpClient
  ) {}

  getReadings(
    sensorId?: number,
    communityId?: number,
    from?: string,
    to?: string,
    page = 1,
    pageSize = 20
  ): Observable<SensorReadingPagedResult> {

    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (sensorId !== undefined) {
      params = params.set('sensorId', sensorId.toString());
    }

    if (communityId !== undefined) {
      params = params.set('communityId', communityId.toString());
    }

    if (from) {
      params = params.set('from', from);
    }

    if (to) {
      params = params.set('to', to);
    }

    return this.http.get<SensorReadingPagedResult>(
      this.readingsUrl,
      { params }
    );
  }

  getSensorHistory(
    sensorId: number,
    from?: string,
    to?: string,
    page = 1,
    pageSize = 20
  ): Observable<SensorReadingPagedResult> {

    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (from) {
      params = params.set('from', from);
    }

    if (to) {
      params = params.set('to', to);
    }

    return this.http.get<SensorReadingPagedResult>(
      `${this.readingsUrl}/sensor/${sensorId}`,
      { params }
    );
  }

  getLatestReadings(
    communityId?: number
  ): Observable<SensorReading[]> {

    let params = new HttpParams();

    if (communityId !== undefined) {
      params = params.set(
        'communityId',
        communityId.toString()
      );
    }

    return this.http.get<SensorReading[]>(
      `${this.readingsUrl}/latest`,
      { params }
    );
  }
}