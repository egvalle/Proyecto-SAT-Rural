import { Routes } from '@angular/router';

export const routes: Routes = [
{
    path: '',
    pathMatch: 'full',
    redirectTo: 'dashboard'
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./features/administration/pages/login/login')
        .then(m => m.Login)
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/monitoring/pages/dashboard/dashboard')
        .then(m => m.Dashboard)
  },
  {
    path: 'sensors',
    loadComponent: () =>
      import('./features/administration/pages/sensors/sensors')
        .then(m => m.Sensors)
  },
  {
    path: 'alerts',
    loadComponent: () =>
      import('./features/alerts/pages/alerts/alerts')
        .then(m => m.Alerts)
  },
  {
    path: 'history',
    loadComponent: () =>
      import('./features/alerts/pages/history/history')
        .then(m => m.History)
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
