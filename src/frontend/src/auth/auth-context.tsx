import { createContext, useContext, useState, useCallback, type ReactNode } from 'react'

export type UserRole = 'Admin' | 'Operator' | 'Viewer'

export interface User {
  id: string
  name: string
  email: string
  role: UserRole
}

export interface AuthContextType {
  user: User | null
  token: string | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (email: string, password: string) => Promise<boolean>
  logout: () => void
  setRole: (role: UserRole) => void
}

const AuthContext = createContext<AuthContextType | null>(null)

// Mock seed users matching Trinetra SRE personas (§10)
export const MOCK_USERS: Record<string, User> = {
  'sre-lead@trinetra.internal': {
    id: 'usr-sre-lead-01',
    name: 'Srikaran R',
    email: 'sre-lead@trinetra.internal',
    role: 'Admin',
  },
  'operator@trinetra.internal': {
    id: 'usr-operator-02',
    name: 'DevOps Operator',
    email: 'operator@trinetra.internal',
    role: 'Operator',
  },
  'viewer@trinetra.internal': {
    id: 'usr-viewer-03',
    name: 'Audit Viewer',
    email: 'viewer@trinetra.internal',
    role: 'Viewer',
  },
}

interface AuthProviderProps {
  children: ReactNode
  /** Initial user for testing and stories */
  initialUser?: User | null
  initialToken?: string | null
}

/**
 * AuthProvider — In-memory auth provider.
 *
 * CRITICAL SECURITY INVARIANT (Brief §10, Decision D-23):
 * "Keep the access token in memory, never in localStorage."
 */
export function AuthProvider({
  children,
  initialUser = MOCK_USERS['sre-lead@trinetra.internal'],
  initialToken = 'mock-jwt-in-memory-token',
}: AuthProviderProps) {
  // In-memory state only — NOT stored in localStorage or sessionStorage
  const [user, setUser] = useState<User | null>(initialUser ?? null)
  const [token, setToken] = useState<string | null>(initialToken ?? null)
  const [isLoading, setIsLoading] = useState(false)

  const login = useCallback(async (email: string, password: string): Promise<boolean> => {
    setIsLoading(true)
    // Small simulated network latency
    await new Promise((resolve) => setTimeout(resolve, 80))

    const normalizedEmail = email.trim().toLowerCase()
    const matchedUser = MOCK_USERS[normalizedEmail]

    if (matchedUser && password.length >= 6) {
      setUser(matchedUser)
      setToken(`mock-jwt-${matchedUser.role.toLowerCase()}-${Date.now()}`)
      setIsLoading(false)
      return true
    }

    // Also accept any valid format email for flexibility if password is correct
    if (normalizedEmail.includes('@') && password === 'password123') {
      const newUser: User = {
        id: `usr-${Date.now()}`,
        name: normalizedEmail.split('@')[0] ?? 'SRE User',
        email: normalizedEmail,
        role: 'Admin',
      }
      setUser(newUser)
      setToken(`mock-jwt-custom-${Date.now()}`)
      setIsLoading(false)
      return true
    }

    setIsLoading(false)
    return false
  }, [])

  const logout = useCallback(() => {
    setUser(null)
    setToken(null)
  }, [])

  const setRole = useCallback(
    (role: UserRole) => {
      if (user) {
        setUser({ ...user, role })
      }
    },
    [user],
  )

  const value: AuthContextType = {
    user,
    token,
    isAuthenticated: !!user && !!token,
    isLoading,
    login,
    logout,
    setRole,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextType {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
