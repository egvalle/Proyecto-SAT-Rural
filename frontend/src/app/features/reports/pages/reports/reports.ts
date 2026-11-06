import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../../core/services/auth.service';
import { environment } from '../../../../../environments/environment';

interface ReportHistoryItem {
  reportId: string;
  type: string;
  fileName: string;
  generatedAt: string;
  generatedBy: { username: string };
  totalAlerts: number;
  totalItems?: number;
}

interface ReportHistoryResponse {
  items: ReportHistoryItem[];
  count: number;
}

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reports.html',
  styleUrl: './reports.scss',
})
export class Reports implements OnInit {
  readonly reportTypes = [
    { type: 'communities', label: 'Comunidades' },
    { type: 'sensors', label: 'Sensores' },
    { type: 'readings', label: 'Lecturas' },
    { type: 'alerts', label: 'Alertas' },
    { type: 'events', label: 'Historial de eventos' },
    { type: 'sensor-status', label: 'Estado de sensores' },
    { type: 'climate', label: 'Estadísticas climáticas' },
    { type: 'executive', label: 'Ejecutivo' },
  ];
  selectedType = 'alerts';
  level = '';
  activeOnly = false;
  communityId: number | null = null;
  sensorId: number | null = null;
  sensorType = '';
  fromDate = '';
  toDate = '';
  loading = false;
  message = '';
  history: ReportHistoryItem[] = [];
  historyLoading = false;
  historyError = '';
  downloadingId: string | null = null;

  private readonly reportUrl =
    `${environment.apiBaseUrl}/api/reports/generate`;
  private readonly historyUrl =
    `${environment.apiBaseUrl}/api/reports/history`;

  constructor(
    private readonly http: HttpClient,
    private readonly authService: AuthService,
    private readonly route: ActivatedRoute,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    const requestedType = this.route.snapshot.queryParamMap.get('type');
    if (requestedType && this.reportTypes.some(item => item.type === requestedType)) {
      this.selectedType = requestedType;
    }
    this.loadHistory();
  }

  private authHeaders(): HttpHeaders | null {
    const token = this.authService.getToken();
    return token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : null;
  }

  loadHistory(): void {
    const headers = this.authHeaders();
    if (!headers) {
      this.historyError = 'La sesión expiró. Inicia sesión nuevamente.';
      return;
    }

    this.historyLoading = true;
    this.historyError = '';
    this.http.get<ReportHistoryResponse>(this.historyUrl, {
      headers,
      params: { limit: '20' },
    }).subscribe({
      next: result => {
        this.history = result.items;
        this.historyLoading = false;
        this.changeDetector.detectChanges();
      },
      error: () => {
        this.historyLoading = false;
        this.historyError = 'No se pudo consultar el historial de reportes.';
        this.changeDetector.detectChanges();
      },
    });
  }

  downloadReport(report: ReportHistoryItem): void {
    const headers = this.authHeaders();
    if (!headers) {
      this.historyError = 'La sesión expiró. Inicia sesión nuevamente.';
      return;
    }

    this.downloadingId = report.reportId;
    this.http.get(
      `${environment.apiBaseUrl}/api/reports/${report.reportId}/download`,
      { headers, responseType: 'blob' },
    ).subscribe({
      next: pdf => {
        this.savePdf(pdf, report.fileName);
        this.downloadingId = null;
        this.changeDetector.detectChanges();
      },
      error: () => {
        this.downloadingId = null;
        this.historyError = 'No se pudo descargar el reporte seleccionado.';
        this.changeDetector.detectChanges();
      },
    });
  }

  private savePdf(pdf: Blob, fileName: string): void {
    const url = URL.createObjectURL(pdf);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    setTimeout(() => URL.revokeObjectURL(url), 0);
  }

  typeLabel(type: string): string {
    return this.reportTypes.find(item => item.type === type)?.label ?? type;
  }

  generateReport(): void {
    const headers = this.authHeaders();
    if (!headers) {
      this.message = 'La sesión expiró. Inicia sesión nuevamente.';
      return;
    }

    this.loading = true;
    this.message = '';

    if (this.selectedType === 'readings' && (!this.fromDate || !this.toDate)) {
      this.message = 'Selecciona las fechas de inicio y fin para el reporte de lecturas.';
      this.loading = false;
      return;
    }

    if (this.fromDate && this.toDate && this.fromDate > this.toDate) {
      this.message = 'La fecha inicial debe ser anterior a la final.';
      this.loading = false;
      return;
    }

    const filters: Record<string, string | number | boolean> = {
      type: this.selectedType,
    };
    if (this.selectedType === 'alerts') {
      if (this.level) filters['level'] = this.level;
      if (this.activeOnly) filters['is_active'] = true;
    }
    if (this.communityId) filters['community_id'] = this.communityId;
    if (this.selectedType === 'readings') {
      if (this.sensorId) filters['sensor_id'] = this.sensorId;
      if (this.sensorType) filters['sensor_type'] = this.sensorType;
      if (this.fromDate) filters['from_date'] = `${this.fromDate}T00:00:00`;
      if (this.toDate) filters['to_date'] = `${this.toDate}T23:59:59`;
    }
    if (this.selectedType === 'events') {
      if (this.fromDate) filters['from_date'] = `${this.fromDate}T00:00:00`;
      if (this.toDate) filters['to_date'] = `${this.toDate}T23:59:59`;
    }
    if (this.selectedType === 'sensors' && this.sensorType) {
      filters['sensor_type'] = this.sensorType;
    }

    this.http.post(
      this.reportUrl,
      filters,
      {
        headers,
        responseType: 'blob',
      },
    ).subscribe({
      next: pdf => {
        this.savePdf(pdf, `reporte-${this.selectedType}.pdf`);
        this.loading = false;
        this.message = 'El reporte se descargó correctamente.';
        this.loadHistory();
        this.changeDetector.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.message = 'No se pudo generar el reporte. Revisa tu sesión y vuelve a intentarlo.';
        this.changeDetector.detectChanges();
      },
    });
  }
}
