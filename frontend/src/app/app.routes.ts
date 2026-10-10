import { Routes } from '@angular/router';
import { MainLayout } from './layout/main-layout/main-layout';
import { authGuard } from './core/guards/auth.guard';

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
    path: 'register',
    loadComponent: () =>
      import('./features/administration/pages/register/register')
        .then(m => m.Register),
    title: 'Crear usuario'
  },

  {
    path: '',
    component: MainLayout,
    canActivate: [authGuard],
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
        path: 'communities',
        loadComponent: () =>
          import('./features/administration/pages/communities/communities')
            .then(m => m.Communities),
        title: 'Comunidades'
      },

      {
        path: 'readings',
        loadComponent: () =>
          import('./features/administration/pages/readings/readings')
            .then(m => m.Readings),
        title: 'Lecturas'
      },

      {
        path: 'alerts',
        loadComponent: () =>
          import('./features/alerts/pages/alerts/alerts')
            .then(m => m.Alerts),
        title: 'Alertas'
      },
      {
        path: 'alert-rules',
        loadComponent: () =>
          import('./features/administration/pages/alert-rules/alert-rules')
            .then(m => m.AlertRules),
        title: 'Reglas de alertas'
      },
      {
        path: 'history',
        loadComponent: () =>
          import('./features/alerts/pages/history/history')
            .then(m => m.History),
        title: 'Historial'
      },
      {
        path: 'user',
        loadComponent: () =>
          import('./features/administration/pages/user/user')
            .then(m => m.User),
        title: 'Usuarios'
      },
      {
        path: 'binnacle',
        loadComponent: () =>
          import('./features/administration/pages/binnacle/binnacle')
            .then(m => m.Binnacle),
        title: 'Bitacora'
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];