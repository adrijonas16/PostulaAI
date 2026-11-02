import type { ApplicationStatus } from '../../shared/api/types'
import { applicationStatusLabels, applicationStatusStyles } from './applicationLabels'

export function ApplicationStatusPill({ status }: { status: ApplicationStatus }) {
  return (
    <span className={`inline-flex rounded-full px-2.5 py-0.5 text-xs font-semibold ${applicationStatusStyles[status]}`}>
      {applicationStatusLabels[status]}
    </span>
  )
}
