import { Component } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../../core/services/auth.service';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './reports.html',
})
export class Reports {
  level = 'RED';
  activeOnly = true;
  loading = false;
  message = '';

  private readonly reportUrl =
    `${environment.apiBaseUrl}/api/reports/alerts`;

  constructor(
    private readonly http: HttpClient,
    private readonly authService: AuthService,
  ) {}

  generateReport(): void {
    const token = this.authService.getToken();

    if (!token) {
      this.message = 'La sesión expiró. Inicia sesión nuevamente.';
      return;
    }

    this.loading = true;
    this.message = '';

    const headers = new HttpHeaders({
      Authorization: `Bearer ${token}`,
    });

    this.http.post(
      this.reportUrl,
      {
        level: this.level,
        is_active: this.activeOnly,
      },
      {
        headers,
        responseType: 'blob',
      },
    ).subscribe({
      next: pdf => {
        const url = URL.createObjectURL(pdf);
        const link = document.createElement('a');
        link.href = url;
        link.download = 'reporte-alertas.pdf';
        link.click();
        URL.revokeObjectURL(url);

        this.loading = false;
        this.message = 'El reporte se descargó correctamente.';
      },
      error: () => {
        this.loading = false;
        this.message = 'No se pudo generar el reporte. Revisa tu sesión y vuelve a intentarlo.';
      },
    });
  }
}