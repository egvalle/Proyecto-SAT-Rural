import { Component } from '@angular/core';

interface RecentAlert {
  id: number;
  title: string;
  message: string;
  time: string;
  level: 'yellow' | 'orange' | 'red';
}

@Component({
  selector: 'app-recent-alerts',
  standalone: true,
  imports: [],
  templateUrl: './recent-alerts.html',
  styleUrl: './recent-alerts.scss'
})
export class RecentAlerts {

  alerts: RecentAlert[] = [
    {
      id: 1,
      title: 'Precaución por lluvia',
      message:
        'Incremento de precipitación detectado en el sector norte.',
      time: '20:42',
      level: 'yellow'
    },
    {
      id: 2,
      title: 'Tormenta moderada',
      message:
        'Se registraron ráfagas de viento superiores al rango habitual.',
      time: '18:16',
      level: 'orange'
    },
    {
      id: 3,
      title: 'Nivel de río en observación',
      message:
        'Incremento del 8% registrado durante la última hora.',
      time: '17:05',
      level: 'yellow'
    }
  ];
}