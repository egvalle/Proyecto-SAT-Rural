import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-system-status',
  standalone: true,
  imports: [],
  templateUrl: './system-status.html',
  styleUrl: './system-status.scss'
})
export class SystemStatus {

  @Input() title = 'Estado general: Normal';

  @Input() description =
    'No se detectan condiciones críticas en la comunidad.';

  @Input() updatedAt =
    'Actualizado hace unos segundos';
}