import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';

import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel
} from '@microsoft/signalr';

import { DashboardData } from '../models/dashboard.model';

@Injectable({
  providedIn: 'root'
})
export class MonitoringService {

  private readonly apiUrl = '/api/monitoring';

  private hubConnection?: HubConnection;

  private readonly readingsUpdatedSubject =
    new Subject<void>();

  readonly readingsUpdated$ =
    this.readingsUpdatedSubject.asObservable();

  constructor(
    private http: HttpClient
  ) {
  }

  getDashboard(): Observable<DashboardData> {
    return this.http.get<DashboardData>(
      `${this.apiUrl}/dashboard`
    );
  }

  async startRealtimeConnection(): Promise<void> {

    console.log('Intentando conectar SignalR...');

    if (this.hubConnection) {
      console.log('SignalR ya estaba inicializado.');
      return;
    }

    this.hubConnection =
      new HubConnectionBuilder()
        .withUrl('/hubs/monitoring')
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Information)
        .build();

    this.hubConnection.on(
      'SensorReadingsUpdated',
      () => {
        console.log('Nueva lectura recibida por SignalR.');

        this.readingsUpdatedSubject.next();
      }
    );

    try {
      await this.hubConnection.start();

      console.log('SignalR conectado correctamente.');
    }
    catch (error) {
      console.error(
        'Error al conectar con SignalR:',
        error
      );

      this.hubConnection = undefined;
    }
  }
}