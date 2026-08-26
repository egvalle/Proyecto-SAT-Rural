import { Component, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';

import { AlertsService } from '../../services/alerts.service';
import { Alert } from '../../models/alert.model';

@Component({
  selector: 'app-alerts',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './alerts.html',
  styleUrl: './alerts.scss',
})
export class Alerts implements OnInit {

  alerts: Alert[] = [];
  loading = true;
  error = false;
  levelFilter: string | null = null;
  activeOnly = true;

  constructor(private alertsService: AlertsService) {
  }

  ngOnInit(): void {
    this.loadAlerts();
  }

  loadAlerts(): void {
    this.loading = true;
    this.error = false;

    this.alertsService
      .getAlerts(this.levelFilter ?? undefined, this.activeOnly ? true : undefined)
      .subscribe({
        next: data => {
          this.alerts = data;
          this.loading = false;
        },
        error: () => {
          this.error = true;
          this.loading = false;
        }
      });
  }

  setLevelFilter(level: string | null): void {
    this.levelFilter = level;
    this.loadAlerts();
  }

  toggleActiveOnly(): void {
    this.activeOnly = !this.activeOnly;
    this.loadAlerts();
  }

  resolve(alert: Alert): void {
    this.alertsService.resolveAlert(alert.id).subscribe({
      next: () => this.loadAlerts()
    });
  }
}