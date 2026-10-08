import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';

import {
  CreateSensorRequest,
  Sensor,
  SensorPagedResult,
  UpdateSensorRequest
} from '../models/sensor';

export interface SensorSimulationResponse {
  sensorId: number;
  sensorCode: string;
  sensorName?: string;
  sensorType?: string;
  value?: number;
  unit?: string;
  mode: 'MANUAL' | 'AUTOMATIC';
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class SensorService {

  private readonly sensorsUrl =
    `${environment.apiBaseUrl}/api/sensors`;

  private readonly monitoringUrl =
    `${environment.apiBaseUrl}/api/Monitoring`;

  constructor(
    private readonly http: HttpClient,
  ) {}

  getSensors(
    search = '',
    code = '',
    type = '',
    communityId?: number,
    isActive?: boolean,
    page = 1,
    pageSize = 10
  ): Observable<SensorPagedResult> {

    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search.trim()) {
      params = params.set(
        'search',
        search.trim()
      );
    }

    if (code.trim()) {
      params = params.set(
        'code',
        code.trim()
      );
    }

    if (type.trim()) {
      params = params.set(
        'type',
        type.trim()
      );
    }

    if (communityId !== undefined) {
      params = params.set(
        'communityId',
        communityId.toString()
      );
    }

    if (isActive !== undefined) {
      params = params.set(
        'isActive',
        isActive.toString()
      );
    }

    return this.http.get<SensorPagedResult>(
      this.sensorsUrl,
      { params }
    );
  }

  getSensor(
    id: number
  ): Observable<Sensor> {

    return this.http.get<Sensor>(
      `${this.sensorsUrl}/${id}`
    );
  }

  createSensor(
    request: CreateSensorRequest
  ): Observable<Sensor> {

    return this.http.post<Sensor>(
      this.sensorsUrl,
      request
    );
  }

  updateSensor(
    id: number,
    request: UpdateSensorRequest
  ): Observable<Sensor> {

    return this.http.put<Sensor>(
      `${this.sensorsUrl}/${id}`,
      request
    );
  }

  changeStatus(
    id: number,
    isActive: boolean
  ): Observable<Sensor> {

    return this.http.patch<Sensor>(
      `${this.sensorsUrl}/${id}/status`,
      { isActive }
    );
  }

  setSimulationValue(
    sensorId: number,
    value: number
  ): Observable<SensorSimulationResponse> {

    return this.http.patch<SensorSimulationResponse>(
      `${this.monitoringUrl}/simulation/sensors/${sensorId}`,
      { value }
    );
  }

  resetSimulationValue(
    sensorId: number
  ): Observable<SensorSimulationResponse> {

    return this.http.delete<SensorSimulationResponse>(
      `${this.monitoringUrl}/simulation/sensors/${sensorId}`
    );
  }
}