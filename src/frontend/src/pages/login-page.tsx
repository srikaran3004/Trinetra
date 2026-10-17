import { useState, type FormEvent } from 'react'
import { useNavigate, useLocation } from 'react-router'
import { Card } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { TextField } from '@/components/ui/text-field'
import { InlineAlert } from '@/components/ui/inline-alert'
import { Badge } from '@/components/ui/badge'
import { TrinetraLogo } from '@/components/ui/trinetra-logo'
import { useAuth } from '@/auth/auth-context'

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const { login, isLoading } = useAuth()

  const [email, setEmail] = useState('sre-lead@trinetra.internal')
  const [password, setPassword] = useState('password123')
  const [error, setError] = useState<string | null>(null)

  interface LocationState {
    from?: {
      pathname?: string
    }
  }

  const fromPath = (location.state as LocationState | null)?.from?.pathname || '/incidents'

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)

    if (!email.trim() || !password) {
      setError('Please enter both email and password.')
      return
    }

    const success = await login(email, password)
    if (success) {
      void navigate(fromPath, { replace: true })
    } else {
      setError(
        'Invalid credentials. For demo access, select one of the pre-configured roles below.',
      )
    }
  }

  function handleQuickRole(roleEmail: string) {
    setEmail(roleEmail)
    setPassword('password123')
    setError(null)
  }

  return (
    <div className="flex items-center justify-center min-h-screen bg-[var(--color-canvas)] p-[var(--space-4)]">
      <Card className="w-full max-w-md p-[var(--space-6)] sm:p-[var(--space-8)] space-y-[var(--space-5)] shadow-[var(--shadow-floating)]">
        {/* Brand Header */}
        <div className="flex flex-col items-center text-center space-y-[var(--space-2)]">
          <TrinetraLogo size={40} />
          <h1 className="text-[var(--text-xl)] font-bold text-[var(--color-text-primary)] m-0">
            Sign In to Trinetra
          </h1>
          <p className="text-[var(--text-xs)] text-[var(--color-text-secondary)] max-w-xs">
            Autonomous IT Incident Response Platform.
          </p>
        </div>

        {/* Demo Mode Badge */}
        <div className="flex justify-center">
          <Badge
            variant="neutral"
            size="sm"
            className="font-mono text-[10px] tracking-wider uppercase"
          >
            Demo Mode &bull; Memory Token Only
          </Badge>
        </div>

        {/* Error Alert */}
        {error && (
          <InlineAlert variant="critical" title="Authentication failed">
            {error}
          </InlineAlert>
        )}

        {/* Login Form */}
        <form
          onSubmit={(e) => {
            void handleSubmit(e)
          }}
          className="space-y-[var(--space-4)]"
        >
          <TextField
            label="Email Address"
            type="email"
            value={email}
            onChange={setEmail}
            placeholder="name@company.internal"
            isRequired
            autoComplete="username"
          />

          <TextField
            label="Password"
            type="password"
            value={password}
            onChange={setPassword}
            placeholder="••••••••"
            isRequired
            autoComplete="current-password"
          />

          <Button
            type="submit"
            variant="primary"
            className="w-full font-medium"
            isLoading={isLoading}
          >
            Sign In to Console
          </Button>
        </form>

        {/* Quick Role Fill Section for Testing & QA */}
        <div className="pt-[var(--space-3)] border-t border-[var(--color-border-subtle)] space-y-[var(--space-2)]">
          <p className="text-[11px] font-medium text-[var(--color-text-tertiary)] uppercase tracking-wider text-center">
            Quick-fill Demo Roles
          </p>
          <div className="grid grid-cols-3 gap-[var(--space-2)]">
            <button
              type="button"
              onClick={() => {
                handleQuickRole('sre-lead@trinetra.internal')
              }}
              className="py-1 px-2 text-[11px] font-medium bg-[var(--color-surface-hover)] hover:bg-[var(--color-surface-pressed)] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] rounded-[var(--radius-xs)] border border-[var(--color-border-subtle)] transition-colors cursor-pointer"
            >
              SRE Lead
            </button>
            <button
              type="button"
              onClick={() => {
                handleQuickRole('operator@trinetra.internal')
              }}
              className="py-1 px-2 text-[11px] font-medium bg-[var(--color-surface-hover)] hover:bg-[var(--color-surface-pressed)] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] rounded-[var(--radius-xs)] border border-[var(--color-border-subtle)] transition-colors cursor-pointer"
            >
              Operator
            </button>
            <button
              type="button"
              onClick={() => {
                handleQuickRole('viewer@trinetra.internal')
              }}
              className="py-1 px-2 text-[11px] font-medium bg-[var(--color-surface-hover)] hover:bg-[var(--color-surface-pressed)] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] rounded-[var(--radius-xs)] border border-[var(--color-border-subtle)] transition-colors cursor-pointer"
            >
              Viewer
            </button>
          </div>
        </div>

        {/* Security Invariant Notice (§10) */}
        <p className="text-[10px] text-[var(--color-text-tertiary)] text-center leading-normal">
          Security invariant: JWT tokens are kept strictly in-memory and never written to
          localStorage.
        </p>
      </Card>
    </div>
  )
}
