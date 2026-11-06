import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom, interval, Subscription } from 'rxjs';

import { Community } from '../../../../core/models/community';
import { CommunityService } from '../../../../core/services/community.service';
import { SensorService } from '../../../../core/services/sensor.service';
import { EventsService } from '../../../../core/services/events.service';
import { AlertsService } from '../../../alerts/services/alerts.service';
import { Alert } from '../../../alerts/models/alert.model';
import { SystemStatus } from '../../components/system-status/system-status';
import { SensorCard } from '../../components/sensor-card/sensor-card';
import { SensorChart } from '../../components/sensor-chart/sensor-chart';
import { RecentAlerts } from '../../components/recent-alerts/recent-alerts';
import { EventSimulator } from '../../components/event-simulator/event-simulator';
import { MonitoringService } from '../../services/monitoring.service';
import { DashboardData } from '../../models/dashboard.model';
import { SensorHistory } from '../../models/monitoring-history.model';
import { RecentAlert } from '../../models/recent-alert.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [FormsModule, SystemStatus, SensorCard, SensorChart, RecentAlerts, EventSimulator],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard implements OnInit, OnDestroy {
  dashboard?: DashboardData;
  history: SensorHistory[] = [];
  recentAlerts: RecentAlert[] = [];
  communities: Community[] = [];
  communityId: number | null = null;
  communityCount = 0;
  activeSensors = 0;
  inactiveSensors = 0;
  activeAlerts = 0;
  eventCount = 0;
  alertLevels = { YELLOW: 0, ORANGE: 0, RED: 0 };
  eventTypes: { name: string; count: number }[] = [];
  loading = true;
  error = false;
  summaryLoading = true;
  summaryError = false;
  private liveSubscription?: Subscription;
  private summarySubscription?: Subscription;
  private summaryRequest = 0;
  private destroyed = false;

  constructor(
    private readonly monitoringService: MonitoringService,
    private readonly communityService: CommunityService,
    private readonly sensorService: SensorService,
    private readonly alertsService: AlertsService,
    private readonly eventsService: EventsService,
    private readonly changeDetector: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    void this.loadCommunities();
    this.refreshMonitoring();
    void this.loadSummary();
    this.liveSubscription = interval(5000).subscribe(() => this.refreshMonitoring(false));
    this.summarySubscription = interval(30000).subscribe(() => void this.loadSummary());
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    this.liveSubscription?.unsubscribe();
    this.summarySubscription?.unsubscribe();
    this.summaryRequest++;
  }

  selectCommunity(): void {
    this.refreshMonitoring();
    void this.loadSummary();
  }

  sensorTypeLabel(type: string): string {
    switch (type) {
      case 'TEMPERATURE': return 'Temperatura';
      case 'HUMIDITY': return 'Humedad';
      case 'WIND_SPEED': return 'Viento';
      case 'RAINFALL': return 'Lluvia';
      case 'RIVER_LEVEL': return 'Nivel del río';
      default: return type;
    }
  }

  private async loadCommunities(): Promise<void> {
    try {
      const communities: Community[] = [];
      let page = 1;
      let totalPages = 1;
      do {
        const result = await firstValueFrom(this.communityService.getCommunities(
          '', '', '', undefined, page, 100));
        communities.push(...result.items);
        this.communityCount = result.totalItems;
        totalPages = result.totalPages;
        page++;
      } while (page <= totalPages);
      this.communities = communities;
    } catch {
      this.communities = [];
    }
    if (!this.destroyed) this.changeDetector.detectChanges();
  }

  private refreshMonitoring(showLoading = true): void {
    this.loadDashboard(showLoading);
    this.loadHistory();
    this.loadAlerts();
  }

  private loadDashboard(showLoading = true): void {
    const selected = this.communityId;
    if (showLoading) this.loading = true;
    this.error = false;
    this.monitoringService.getDashboard(selected ?? undefined).subscribe({
      next: data => {
        if (this.communityId !== selected) return;
        this.dashboard = data;
        this.loading = false;
        this.changeDetector.detectChanges();
      },
      error: () => {
        if (this.communityId !== selected) return;
        this.error = true;
        this.loading = false;
        this.changeDetector.detectChanges();
      }
    });
  }

  private loadHistory(): void {
    const selected = this.communityId;
    this.monitoringService.getHistory(10, selected ?? undefined).subscribe({
      next: data => {
        if (this.communityId !== selected) return;
        this.history = data;
        this.changeDetector.detectChanges();
      },
      error: () => {}
    });
  }

  private loadAlerts(): void {
    const selected = this.communityId;
    this.monitoringService.getRecentAlerts(5, selected ?? undefined).subscribe({
      next: data => {
        if (this.communityId !== selected) return;
        this.recentAlerts = data;
        this.changeDetector.detectChanges();
      },
      error: () => {}
    });
  }

  private async loadSummary(): Promise<void> {
    const requestId = ++this.summaryRequest;
    const communityId = this.communityId ?? undefined;
    this.summaryLoading = true;
    this.summaryError = false;
    try {
      const [active, inactive, events] = await Promise.all([
        firstValueFrom(this.sensorService.getSensors('', '', '', communityId, true, 1, 1)),
        firstValueFrom(this.sensorService.getSensors('', '', '', communityId, false, 1, 1)),
        firstValueFrom(this.eventsService.getEvents(
          communityId === undefined ? {} : { communityId }))
      ]);
      const alerts: Alert[] = [];
      let page = 1;
      while (true) {
        const batch = await firstValueFrom(this.alertsService.getAlerts({
          communityId, page, pageSize: 100
        }));
        alerts.push(...batch);
        if (batch.length < 100) break;
        page++;
      }
      if (requestId !== this.summaryRequest) return;
      this.activeSensors = active.totalItems;
      this.inactiveSensors = inactive.totalItems;
      this.activeAlerts = alerts.filter(alert => alert.isActive).length;
      this.eventCount = events.length;
      this.alertLevels = {
        YELLOW: alerts.filter(alert => alert.level === 'YELLOW').length,
        ORANGE: alerts.filter(alert => alert.level === 'ORANGE').length,
        RED: alerts.filter(alert => alert.level === 'RED').length
      };
      const types = new Map<string, number>();
      for (const event of events) {
        types.set(event.eventType, (types.get(event.eventType) ?? 0) + 1);
      }
      this.eventTypes = [...types].map(([name, count]) => ({ name, count }))
        .sort((a, b) => b.count - a.count);
    } catch {
      if (requestId !== this.summaryRequest) return;
      this.summaryError = true;
    } finally {
      if (requestId === this.summaryRequest) {
        this.summaryLoading = false;
        this.changeDetector.detectChanges();
      }
    }
  }
}
