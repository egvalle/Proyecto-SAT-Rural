import {
  Component,
  OnDestroy,
  OnInit
} from '@angular/core';

import { Subscription } from 'rxjs';

import { SystemStatus }
  from '../../components/system-status/system-status';

import { SensorCard }
  from '../../components/sensor-card/sensor-card';

import { SensorChart }
  from '../../components/sensor-chart/sensor-chart';

import { RecentAlerts }
  from '../../components/recent-alerts/recent-alerts';

import { EventSimulator }
  from '../../components/event-simulator/event-simulator';

import { MonitoringService }
  from '../../services/monitoring.service';

import { DashboardData }
  from '../../models/dashboard.model';

import { SensorHistory }
  from '../../models/monitoring-history.model';

import { RecentAlert }
  from '../../models/recent-alert.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    SystemStatus,
    SensorCard,
    SensorChart,
    RecentAlerts,
    EventSimulator
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard implements OnInit, OnDestroy {

  dashboard?: DashboardData;
  history: SensorHistory[] = [];
  recentAlerts: RecentAlert[] = [];
  loading = true;
  error = false;

  private readingsSubscription?: Subscription;

  constructor(
    private monitoringService: MonitoringService
  ) {
  }

  ngOnInit(): void {

    this.loadDashboard();
    this.loadHistory();
    this.loadAlerts();

    this.readingsSubscription =
      this.monitoringService
        .readingsUpdated$
        .subscribe(() => {
          this.loadDashboard(false);
          this.loadHistory();
          this.loadAlerts();
        });

    this.monitoringService
      .startRealtimeConnection();
  }

  ngOnDestroy(): void {
    this.readingsSubscription
      ?.unsubscribe();
  }

  private loadDashboard(
    showLoading = true
  ): void {
    if (showLoading) {
      this.loading = true;
    }
    this.error = false;

    this.monitoringService
      .getDashboard()
      .subscribe({
        next: data => {
          this.dashboard = data;
          this.loading = false;
        },
        error: error => {
          console.error(
            'Error al cargar el dashboard',
            error
          );
          this.error = true;
          this.loading = false;
        }
      });
  }

  private loadHistory(): void {
    this.monitoringService
      .getHistory(10)
      .subscribe({
        next: data => {
          this.history = data;
        },
        error: error => {
          console.error(
            'Error cargando histórico',
            error
          );
        }
      });
  }

  private loadAlerts(): void {
    this.monitoringService
      .getRecentAlerts(5)
      .subscribe({
        next: data => {
          this.recentAlerts = data;
        },
        error: error => {
          console.error(
            'Error cargando alertas',
            error
          );
        }
      });
  }

}