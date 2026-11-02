import type { RouteObject } from 'react-router'
import { AnalysisHistoryPage } from '../features/analyses/AnalysisHistoryPage'
import { AnalysisResultPage } from '../features/analyses/AnalysisResultPage'
import { RunAnalysisPage } from '../features/analyses/RunAnalysisPage'
import { LoginPage } from '../features/auth/LoginPage'
import { RegisterPage } from '../features/auth/RegisterPage'
import { DashboardPage } from '../features/dashboard/DashboardPage'
import { JobPostingEditorPage } from '../features/jobs/JobPostingEditorPage'
import { LandingPage } from '../features/landing/LandingPage'
import { ProfilePage } from '../features/profile/ProfilePage'
import { SettingsPage } from '../features/settings/SettingsPage'
import { AppLayout } from './AppLayout'
import { NotFoundPage } from './NotFoundPage'
import { PublicLayout } from './PublicLayout'

export const paths = {
  home: '/',
  login: '/iniciar-sesion',
  register: '/registro',
  dashboard: '/app',
  profile: '/app/perfil',
  newJobPosting: '/app/vacantes/nueva',
  editJobPosting: (id: string) => `/app/vacantes/${id}/editar`,
  runAnalysis: '/app/analisis/nuevo',
  analysisHistory: '/app/analisis',
  analysisResult: (id: string) => `/app/analisis/${id}`,
  settings: '/app/configuracion',
} as const

// Authentication guards are added in Phase 1; until then /app routes are placeholders.
export const routes: RouteObject[] = [
  {
    element: <PublicLayout />,
    children: [
      { index: true, element: <LandingPage /> },
      { path: paths.login, element: <LoginPage /> },
      { path: paths.register, element: <RegisterPage /> },
    ],
  },
  {
    path: paths.dashboard,
    element: <AppLayout />,
    children: [
      { index: true, element: <DashboardPage /> },
      { path: 'perfil', element: <ProfilePage /> },
      { path: 'vacantes/nueva', element: <JobPostingEditorPage /> },
      { path: 'vacantes/:jobPostingId/editar', element: <JobPostingEditorPage /> },
      { path: 'analisis', element: <AnalysisHistoryPage /> },
      { path: 'analisis/nuevo', element: <RunAnalysisPage /> },
      { path: 'analisis/:analysisId', element: <AnalysisResultPage /> },
      { path: 'configuracion', element: <SettingsPage /> },
    ],
  },
  { path: '*', element: <NotFoundPage /> },
]
