import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/dashboard/dashboard.component').then(
        (m) => m.DashboardComponent
      ),
  },
  {
    path: 'incidents',
    loadComponent: () =>
      import('./features/incidents/incidents-list.component').then(
        (m) => m.IncidentsListComponent
      ),
  },
  {
    path: 'incidents/:id',
    loadComponent: () =>
      import('./features/incidents/incident-detail.component').then(
        (m) => m.IncidentDetailComponent
      ),
  },
  {
    path: 'approvals',
    loadComponent: () =>
      import('./features/approvals/approvals.component').then(
        (m) => m.ApprovalsComponent
      ),
  },
  { path: '**', redirectTo: 'dashboard' },
];
