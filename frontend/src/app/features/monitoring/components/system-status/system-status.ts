import { Component, Input } from '@angular/core';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-system-status',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './system-status.html',
  styleUrl: './system-status.scss'
})
export class SystemStatus {

  @Input() title = 'Estado general: Normal';

  @Input() description =
    'No se detectan condiciones críticas en la comunidad.';

  @Input() updatedAt = '';

  @Input() status = 'GREEN';

  get statusClass(): string {
    return this.status.toLowerCase();
  }
}