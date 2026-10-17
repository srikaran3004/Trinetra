import { describe, it, expect, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router'
import { AuthProvider, useAuth } from '@/auth/auth-context'
import { ProtectedRoute } from '@/auth/protected-route'
import { LoginPage } from '@/pages/login-page'

// Test Consumer Component
function TestAuthConsumer() {
  const { user, token, isAuthenticated, logout } = useAuth()
  return (
    <div>
      <div data-testid="auth-status">{isAuthenticated ? 'authenticated' : 'unauthenticated'}</div>
      <div data-testid="user-email">{user?.email ?? 'none'}</div>
      <div data-testid="user-role">{user?.role ?? 'none'}</div>
      <div data-testid="has-token">{token ? 'yes' : 'no'}</div>
      <button type="button" onClick={logout}>
        Log out
      </button>
    </div>
  )
}

describe('Auth System & Route Guard (Task F2-2)', () => {
  beforeEach(() => {
    localStorage.clear()
    sessionStorage.clear()
  })

  describe('AuthProvider Invariant (§10 & Decision D-23)', () => {
    it('maintains token strictly in memory and NEVER writes to localStorage or sessionStorage', () => {
      render(
        <AuthProvider>
          <TestAuthConsumer />
        </AuthProvider>,
      )

      expect(screen.getByTestId('auth-status')).toHaveTextContent('authenticated')
      expect(screen.getByTestId('has-token')).toHaveTextContent('yes')

      // Assert localStorage and sessionStorage are completely clean of tokens
      expect(localStorage.getItem('token')).toBeNull()
      expect(localStorage.getItem('access_token')).toBeNull()
      expect(localStorage.getItem('jwt')).toBeNull()
      expect(sessionStorage.getItem('token')).toBeNull()
    })

    it('clears in-memory token and user state on logout', async () => {
      const user = userEvent.setup()
      render(
        <AuthProvider>
          <TestAuthConsumer />
        </AuthProvider>,
      )

      await user.click(screen.getByRole('button', { name: /log out/i }))

      expect(screen.getByTestId('auth-status')).toHaveTextContent('unauthenticated')
      expect(screen.getByTestId('user-email')).toHaveTextContent('none')
      expect(screen.getByTestId('has-token')).toHaveTextContent('no')
    })
  })

  describe('ProtectedRoute Route Guard', () => {
    it('redirects unauthenticated user to /login', () => {
      render(
        <AuthProvider initialUser={null} initialToken={null}>
          <MemoryRouter initialEntries={['/protected']}>
            <Routes>
              <Route path="/login" element={<div>Login Page Mock</div>} />
              <Route element={<ProtectedRoute />}>
                <Route path="/protected" element={<div>Secret Console Data</div>} />
              </Route>
            </Routes>
          </MemoryRouter>
        </AuthProvider>,
      )

      expect(screen.getByText('Login Page Mock')).toBeInTheDocument()
      expect(screen.queryByText('Secret Console Data')).not.toBeInTheDocument()
    })

    it('renders protected content when user is authenticated', () => {
      render(
        <AuthProvider>
          <MemoryRouter initialEntries={['/protected']}>
            <Routes>
              <Route path="/login" element={<div>Login Page Mock</div>} />
              <Route element={<ProtectedRoute />}>
                <Route path="/protected" element={<div>Secret Console Data</div>} />
              </Route>
            </Routes>
          </MemoryRouter>
        </AuthProvider>,
      )

      expect(screen.getByText('Secret Console Data')).toBeInTheDocument()
    })
  })

  describe('LoginPage Component', () => {
    it('renders sign in card with email and password fields', () => {
      render(
        <AuthProvider initialUser={null} initialToken={null}>
          <MemoryRouter initialEntries={['/login']}>
            <LoginPage />
          </MemoryRouter>
        </AuthProvider>,
      )

      expect(
        screen.getByRole('heading', { level: 1, name: /sign in to trinetra/i }),
      ).toBeInTheDocument()
      expect(screen.getByLabelText(/email address/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/password/i)).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /sign in to console/i })).toBeInTheDocument()
    })

    it('populates fields when clicking demo quick-role buttons', async () => {
      const user = userEvent.setup()
      render(
        <AuthProvider initialUser={null} initialToken={null}>
          <MemoryRouter initialEntries={['/login']}>
            <LoginPage />
          </MemoryRouter>
        </AuthProvider>,
      )

      const operatorBtn = screen.getByRole('button', { name: 'Operator' })
      await user.click(operatorBtn)

      expect(screen.getByLabelText(/email address/i)).toHaveValue('operator@trinetra.internal')

      const viewerBtn = screen.getByRole('button', { name: 'Viewer' })
      await user.click(viewerBtn)

      expect(screen.getByLabelText(/email address/i)).toHaveValue('viewer@trinetra.internal')
    })

    it('authenticates and redirects on valid credentials submission', async () => {
      const user = userEvent.setup()
      render(
        <AuthProvider initialUser={null} initialToken={null}>
          <MemoryRouter initialEntries={['/login']}>
            <Routes>
              <Route path="/login" element={<LoginPage />} />
              <Route path="/incidents" element={<div>Incidents Console Live</div>} />
            </Routes>
          </MemoryRouter>
        </AuthProvider>,
      )

      await user.click(screen.getByRole('button', { name: /sign in to console/i }))

      await waitFor(() => {
        expect(screen.getByText('Incidents Console Live')).toBeInTheDocument()
      })
    })

    it('displays error alert on invalid credentials submission', async () => {
      const user = userEvent.setup()
      render(
        <AuthProvider initialUser={null} initialToken={null}>
          <MemoryRouter initialEntries={['/login']}>
            <LoginPage />
          </MemoryRouter>
        </AuthProvider>,
      )

      const emailInput = screen.getByLabelText(/email address/i)
      const passwordInput = screen.getByLabelText(/password/i)

      await user.clear(emailInput)
      await user.type(emailInput, 'invalid@example.com')
      await user.clear(passwordInput)
      await user.type(passwordInput, 'wrongpass')

      await user.click(screen.getByRole('button', { name: /sign in to console/i }))

      await waitFor(() => {
        expect(screen.getByText(/invalid credentials/i)).toBeInTheDocument()
      })
    })
  })
})
