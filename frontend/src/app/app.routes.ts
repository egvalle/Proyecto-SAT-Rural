import { Routes } from '@angular/router';
import { MainLayout } from './layout/main-layout/main-layout';

export const routes: Routes = [

  {
    path: 'login',
    loadComponent: () =>
      import('./features/administration/pages/login/login')
        .then(m => m.Login)
  },

  {
    path: '',
    component: MainLayout,
    children: [

      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
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
      }

    ]
  },

  {
    path: '**',
    redirectTo: 'dashboard'
  }

];