import { Routes } from '@angular/router';
import { MainLayout } from './layout/main-layout/main-layout';

export const routes: Routes = [

  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'login'
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./features/administration/pages/login/login')
        .then(m => m.Login),
    title: 'Login'
  },

  {
    path: '',
    component: MainLayout,
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/monitoring/pages/dashboard/dashboard')
            .then(m => m.Dashboard),
        title: 'Dashboard'
      },
      {
        path: 'sensors',
        loadComponent: () =>
          import('./features/administration/pages/sensors/sensors')
            .then(m => m.Sensors),
        title: 'Sensores'
      },
      {
        path: 'alerts',
        loadComponent: () =>
          import('./features/alerts/pages/alerts/alerts')
            .then(m => m.Alerts),
        title: 'Alertas'
      },
      {
        path: 'history',
        loadComponent: () =>
          import('./features/alerts/pages/history/history')
            .then(m => m.History),
        title: 'Historial'
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'login'
  }
];