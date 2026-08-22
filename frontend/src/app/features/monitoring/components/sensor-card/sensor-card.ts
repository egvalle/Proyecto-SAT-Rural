import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-sensor-card',
  standalone: true,
  imports: [],
  templateUrl: './sensor-card.html',
  styleUrl: './sensor-card.scss'
})
export class SensorCard {

  @Input() title = '';
  @Input() value: number | string = '';
  @Input() unit = '';

  @Input() icon = 'bi bi-broadcast';

  @Input() status = 'Normal';

  @Input() statusClass:
    'normal' | 'warning' | 'alert' | 'emergency' = 'normal';
}