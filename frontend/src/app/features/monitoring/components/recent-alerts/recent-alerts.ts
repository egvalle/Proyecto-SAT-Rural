import { Component, Input } from '@angular/core';
import { DatePipe } from '@angular/common';

import { RecentAlert }
  from '../../models/recent-alert.model';

@Component({
  selector: 'app-recent-alerts',
  standalone: true,
  imports: [
    DatePipe
  ],
  templateUrl: './recent-alerts.html',
  styleUrl: './recent-alerts.scss'
})
export class RecentAlerts {

  @Input() alerts: RecentAlert[] = [];

}