import { Routes } from '@angular/router';

import { Dashboard } from './features/monitoring/pages/dashboard/dashboard';
import { Login } from './features/administration/pages/login/login';
import { Home } from './home/home';

export const routes: Routes = [
  {
    path: '',
    component: Home,
    title: 'Home',
  },
  {
    path: 'login',
    component: Login,
    title: 'Login',
  },
   {
    path: 'dashboard',
    component: Dashboard,
    title: 'Dashboard',
  },
  {
    path: 'sensors',
    loadComponent: () =>
      import('./features/administration/pages/sensors/sensors')
        .then(m => m.Sensors),
    title: 'Sensors',
  },
  // {
  //   path: 'alerts',
  //   loadComponent: () =>
  //     import('./features/alerts/pages/alerts/alerts')
  //       .then(m => m.Alerts)
  // },
  // {
  //   path: 'history',
  //   loadComponent: () =>
  //     import('./features/alerts/pages/history/history')
  //       .then(m => m.History)
  // },
  // {
  //   path: '**',
  //   redirectTo: 'dashboard'
  // }
];
