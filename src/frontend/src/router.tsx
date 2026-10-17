import { createBrowserRouter, Navigate } from 'react-router'
import { ConsoleLayout } from './components/layout/console-layout'
import { ProtectedRoute } from './auth/protected-route'
import { IncidentsPage } from './pages/incidents-page'
import { IncidentHistoryPage } from './pages/incident-history-page'
import { IncidentDetailPage } from './pages/incident-detail-page'
import { AgentsPage } from './pages/agents-page'
import { ApprovalsPage } from './pages/approvals-page'
import { RunbooksPage } from './pages/runbooks-page'
import { SettingsPage } from './pages/settings-page'
import { LoginPage } from './pages/login-page'
import { NotFoundPage } from './pages/not-found-page'

export const router = createBrowserRouter([
  {
    path: '/login',
    element: <LoginPage />,
  },
  {
    element: <ProtectedRoute />,
    children: [
      {
        path: '/',
        element: <ConsoleLayout />,
        children: [
          {
            index: true,
            element: <Navigate to="/incidents" replace />,
          },
          {
            path: 'incidents',
            element: <IncidentsPage />,
          },
          {
            path: 'incidents/history',
            element: <IncidentHistoryPage />,
          },
          {
            path: 'incidents/:id',
            element: <IncidentDetailPage />,
          },
          {
            path: 'agents',
            element: <AgentsPage />,
          },
          {
            path: 'approvals',
            element: <ApprovalsPage />,
          },
          {
            path: 'runbooks',
            element: <RunbooksPage />,
          },
          {
            path: 'settings',
            element: <SettingsPage />,
          },
          {
            path: '*',
            element: <NotFoundPage />,
          },
        ],
      },
    ],
  },
])
