import { environment } from '../../../environments/environment';
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Sensor } from '../../core/models/sensor';
import { Observable, map } from 'rxjs';

export interface SensorListItem extends Sensor {
  location?: string;
  latestReading?: string | number;
}

interface SensorListResponse {
  items?: SensorListItem[];
  data?: SensorListItem[];
  results?: SensorListItem[];
}

@Injectable({
  providedIn: 'root'
})
export class SensorService {

  private readonly sensorsUrl =
    `${environment.apiBaseUrl}/api/sensors`;

  constructor(
    private readonly http: HttpClient
  ) {}

  createSensor(sensor: Sensor): Observable<unknown> {
    return this.http.post<unknown>(
      this.sensorsUrl,
      sensor
    );
  }

  getSensors(
    sensor: string,
    isActive: boolean,
    page = 1,
    pageSize = 10
  ): Observable<SensorListItem[]> {
    const params = new HttpParams()
      .set('sensor', sensor)
      .set('isActive', isActive)
      .set('page', page)
      .set('pageSize', pageSize);

    return this.http.get<SensorListItem[] | SensorListResponse>(this.sensorsUrl, { params }).pipe(
      map(response => {
        if (Array.isArray(response)) {
          return response;
        }

        return response.items ?? response.data ?? response.results ?? [];
      })
    );
  }
}