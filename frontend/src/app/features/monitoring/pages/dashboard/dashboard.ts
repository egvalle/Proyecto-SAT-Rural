import {
  Component,
  ChangeDetectorRef,
  OnDestroy,
  OnInit
} from '@angular/core';

import { interval, Subscription } from 'rxjs';

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
import { LiveHumidityReading } from '../../services/monitoring.service';

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
  realHumidity: LiveHumidityReading | null = null;
  private liveSubscription?: Subscription;

  constructor(
    private monitoringService: MonitoringService,
    private readonly changeDetector: ChangeDetectorRef
  ) {
  }

  ngOnInit(): void {

    this.loadDashboard();
    this.loadHistory();
    this.loadAlerts();
    this.loadLiveData();
    this.liveSubscription = interval(5000).subscribe(() => {
      this.loadDashboard(false);
      this.loadHistory();
      this.loadAlerts();
      this.loadLiveData();
    });
  }

  ngOnDestroy(): void {
    this.liveSubscription?.unsubscribe();
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
          this.changeDetector.detectChanges();
        },
        error: error => {
          console.error(
            'Error al cargar el dashboard',
            error
          );
          this.error = true;
          this.loading = false;
          this.changeDetector.detectChanges();
        }
      });
  }

  private loadHistory(): void {
    this.monitoringService
      .getHistory(10)
      .subscribe({
        next: data => {
          this.history = data;
          this.changeDetector.detectChanges();
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
          this.changeDetector.detectChanges();
        },
        error: error => {
          console.error(
            'Error cargando alertas',
            error
          );
        }
      });
  }

  private loadLiveData(): void {
    this.monitoringService.getLiveHumidity().subscribe({
      next: readings => {
        const now = Date.now();
        this.realHumidity = readings.find(item =>
          item.source === 'REAL' &&
          now - Date.parse(item.recordedAt) < 30000 &&
          now - Date.parse(item.recordedAt) >= 0) ?? null;
        this.changeDetector.detectChanges();
      },
      error: () => {
        this.realHumidity = null;
        this.changeDetector.detectChanges();
      }
    });
  }

}
