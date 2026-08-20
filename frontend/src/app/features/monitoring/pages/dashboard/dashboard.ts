import { Component } from '@angular/core';

import { SystemStatus }
  from '../../components/system-status/system-status';

import { SensorCard }
  from '../../components/sensor-card/sensor-card';

import { SensorChart }
  from '../../components/sensor-chart/sensor-chart';

import { RecentAlerts }
  from '../../components/recent-alerts/recent-alerts';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    SystemStatus,
    SensorCard,
    SensorChart,
    RecentAlerts
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard {
}