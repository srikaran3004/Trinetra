import { useNavigate } from 'react-router'
import { EmptyState } from '@/components/ui/empty-state'
import { Button } from '@/components/ui/button'
import { AlertCircle } from 'lucide-react'

export function NotFoundPage() {
  const navigate = useNavigate()

  return (
    <div className="flex items-center justify-center min-h-[60vh] p-[var(--space-6)]">
      <EmptyState
        icon={<AlertCircle size={32} strokeWidth={1.5} />}
        title="Page Not Found"
        description="The requested console route does not exist or has been moved."
        action={
          <Button
            variant="primary"
            onPress={() => {
              void navigate('/incidents')
            }}
          >
            Back to Incidents
          </Button>
        }
      />
    </div>
  )
}
