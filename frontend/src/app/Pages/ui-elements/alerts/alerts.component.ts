import { Component } from '@angular/core';
import { AlertComponent } from '../../../shared/components/ui/alert/alert.component';

import { ComponentCardComponent } from '../../..//shared/component-card/component-card.component';
import { PageBreadcrumbComponent } from '../../../shared/page-breadcrumb/page-breadcrumb.component';

@Component({
  selector: 'app-alerts',
  imports: [
    AlertComponent,
    ComponentCardComponent,
    PageBreadcrumbComponent,
  ],
  templateUrl: './alerts.component.html',
  styles: ``
})
export class AlertsComponent {

}
