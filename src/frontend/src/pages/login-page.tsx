import { useNavigate } from 'react-router'
import { Card } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { TrinetraLogo } from '@/components/ui/trinetra-logo'

export function LoginPage() {
  const navigate = useNavigate()

  return (
    <div className="flex items-center justify-center min-h-screen bg-[var(--color-canvas)] p-[var(--space-4)]">
      <Card className="w-full max-w-sm p-[var(--space-6)] space-y-[var(--space-4)] text-center">
        <div className="flex justify-center">
          <TrinetraLogo size={36} showText />
        </div>
        <div>
          <h2 className="text-[var(--text-lg)] font-bold text-[var(--color-text-primary)]">
            Sign In
          </h2>
          <p className="text-[var(--text-xs)] text-[var(--color-text-secondary)] mt-1">
            Autonomous IT incident response platform
          </p>
        </div>
        <Button
          variant="primary"
          className="w-full"
          onPress={() => {
            void navigate('/incidents')
          }}
        >
          Sign In to Console
        </Button>
      </Card>
    </div>
  )
}
