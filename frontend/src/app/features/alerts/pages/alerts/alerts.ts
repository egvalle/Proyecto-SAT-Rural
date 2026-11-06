import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { AlertFilters, AlertsService } from '../../services/alerts.service';
import { Alert } from '../../models/alert.model';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-alerts',
  standalone: true,
  imports: [DatePipe, FormsModule],
  templateUrl: './alerts.html',
  styleUrl: './alerts.scss',
})
export class Alerts implements OnInit {

  alerts: Alert[] = [];
  loading = true;
  error = false;
  actionError = '';
  levelFilter: string | null = null;
  statusFilter = '';
  communityId: number | null = null;
  sensorId: number | null = null;
  phenomenon = '';
  dateFrom = '';
  dateTo = '';
  page = 1;
  readonly pageSize = 20;
  hasMore = false;
  expandedAlert: Alert | null = null;
  loadingDetails = false;
  detailsError = false;

  constructor(
    private alertsService: AlertsService,
    readonly authService: AuthService,
    private readonly changeDetector: ChangeDetectorRef
  ) {
  }

  get canWrite(): boolean {
    return this.authService.canWrite();
  }

  ngOnInit(): void {
    this.loadAlerts();
  }

  loadAlerts(): void {
    this.loading = true;
    this.error = false;
    this.expandedAlert = null;

    const filters: AlertFilters = {
      level: this.levelFilter ?? undefined,
      status: this.statusFilter || undefined,
      communityId: this.communityId ?? undefined,
      sensorId: this.sensorId ?? undefined,
      phenomenon: this.phenomenon.trim() || undefined,
      from: this.dateFrom || undefined,
      to: this.dateTo || undefined,
      page: this.page,
      pageSize: this.pageSize
    };

    this.alertsService
      .getAlerts(filters)
      .subscribe({
        next: data => {
          this.alerts = data;
          this.hasMore = data.length === this.pageSize;
          this.loading = false;
          this.changeDetector.detectChanges();
        },
        error: () => {
          this.error = true;
          this.loading = false;
          this.changeDetector.detectChanges();
        }
      });
  }

  setLevelFilter(level: string | null): void {
    this.levelFilter = level;
    this.page = 1;
    this.loadAlerts();
  }

  applyFilters(): void {
    this.page = 1;
    this.loadAlerts();
  }

  clearFilters(): void {
    this.levelFilter = null;
    this.statusFilter = '';
    this.communityId = null;
    this.sensorId = null;
    this.phenomenon = '';
    this.dateFrom = '';
    this.dateTo = '';
    this.applyFilters();
  }

  changePage(page: number): void {
    if (page < 1) return;
    this.page = page;
    this.loadAlerts();
  }

  showDetails(alert: Alert): void {
    if (this.expandedAlert?.id === alert.id) {
      this.expandedAlert = null;
      return;
    }
    this.loadingDetails = true;
    this.detailsError = false;
    this.expandedAlert = null;
    this.alertsService.getAlert(alert.id).subscribe({
      next: details => {
        this.expandedAlert = details;
        this.loadingDetails = false;
        this.changeDetector.detectChanges();
      },
      error: () => {
        this.loadingDetails = false;
        this.detailsError = true;
        this.changeDetector.detectChanges();
      }
    });
  }

  statusLabel(status: Alert['status']): string {
    switch (status) {
      case 'ATTENDED': return 'Atendida';
      case 'CLOSED': return 'Cerrada';
      default: return 'Activa';
    }
  }

  attend(alert: Alert): void {
    if (!this.canWrite || alert.status !== 'ACTIVE') return;
    this.actionError = '';
    this.alertsService.attendAlert(alert.id).subscribe({
      next: () => this.loadAlerts(),
      error: () => {
        this.actionError = 'No fue posible marcar la alerta como atendida.';
        this.changeDetector.detectChanges();
      }
    });
  }

  resolve(alert: Alert): void {
    if (!this.canWrite) {
      return;
    }

    this.actionError = '';
    this.alertsService.resolveAlert(alert.id).subscribe({
      next: () => this.loadAlerts(),
      error: () => {
        this.actionError = 'No fue posible resolver la alerta.';
        this.changeDetector.detectChanges();
      }
    });
  }
}
