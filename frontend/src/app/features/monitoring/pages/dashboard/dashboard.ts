import { Component, OnDestroy, OnInit } from '@angular/core';

import { Subscription } from 'rxjs';

import { SystemStatus }
  from '../../components/system-status/system-status';

import { SensorCard }
  from '../../components/sensor-card/sensor-card';

import { SensorChart }
  from '../../components/sensor-chart/sensor-chart';

import { RecentAlerts }
  from '../../components/recent-alerts/recent-alerts';

import { MonitoringService }
  from '../../services/monitoring.service';

import { DashboardData }
  from '../../models/dashboard.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    SystemStatus,
    SensorCard,
    SensorChart,
    RecentAlerts
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard implements OnInit, OnDestroy {

  dashboard?: DashboardData;

  loading = true;

  error = false;

  private readingsSubscription?: Subscription;

  constructor(
    private monitoringService: MonitoringService
  ) {
  }

  ngOnInit(): void {

    this.loadDashboard();

    this.readingsSubscription =
      this.monitoringService
        .readingsUpdated$
        .subscribe(() => {
          this.loadDashboard(false);
        });

    this.monitoringService
      .startRealtimeConnection();
  }

  ngOnDestroy(): void {

    this.readingsSubscription?.unsubscribe();

  }

  private loadDashboard(showLoading = true): void {

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
}