import { describe, it, expect, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import App from '@/App'
import { useSidebarStore } from '@/store/sidebar'

describe('Console Shell Layout & Navigation (Task F2-1)', () => {
  beforeEach(() => {
    // Reset sidebar store state
    useSidebarStore.setState({ isCollapsed: false, isMobileOpen: false })
    window.history.pushState({}, '', '/incidents')
  })

  describe('Sidebar Navigation', () => {
    it('renders all main navigation links per Notion IA (§4.6)', () => {
      render(<App />)

      expect(screen.getByRole('link', { name: /incidents/i })).toBeInTheDocument()
      expect(screen.getByRole('link', { name: /agents/i })).toBeInTheDocument()
      expect(screen.getByRole('link', { name: /approvals/i })).toBeInTheDocument()
      expect(screen.getByRole('link', { name: /runbooks/i })).toBeInTheDocument()
      expect(screen.getByRole('link', { name: /settings/i })).toBeInTheDocument()
    })

    it('marks active navigation item with distinct background and border indicator (not color alone)', () => {
      render(<App />)

      const incidentsLink = screen.getByRole('link', { name: /incidents/i })
      // Verify active styling has both background and border indicator
      expect(incidentsLink.className).toContain('bg-[var(--color-surface-hover)]')
      expect(incidentsLink.className).toContain('border-[var(--color-accent)]')
    })

    it('toggles collapse and expand states on button click', async () => {
      const user = userEvent.setup()
      render(<App />)

      const collapseBtn = screen.getByRole('button', { name: /collapse sidebar/i })
      expect(collapseBtn).toBeInTheDocument()

      await user.click(collapseBtn)

      // Should now show expand button
      expect(screen.getByRole('button', { name: /expand sidebar/i })).toBeInTheDocument()
    })

    it('renders user profile and sign out in sidebar footer', () => {
      render(<App />)

      expect(screen.getByText('Srikaran R')).toBeInTheDocument()
      expect(screen.getByText('SRE Lead')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /sign out/i })).toBeInTheDocument()
    })
  })

  describe('Topbar Controls', () => {
    it('renders breadcrumb title and demo data environment badge', () => {
      render(<App />)

      expect(
        screen.getByRole('heading', { level: 1, name: 'Active Incidents' }),
      ).toBeInTheDocument()
      expect(screen.getByText(/demo data/i)).toBeInTheDocument()
    })

    it('renders real-time connection status indicator', () => {
      render(<App />)

      expect(screen.getByRole('status', { name: /signalr connected/i })).toBeInTheDocument()
    })

    it('renders global search trigger button', () => {
      render(<App />)

      expect(screen.getByRole('button', { name: /search console/i })).toBeInTheDocument()
    })
  })

  describe('Command Palette & Search', () => {
    it('opens command palette when clicking search trigger', async () => {
      const user = userEvent.setup()
      render(<App />)

      const searchBtn = screen.getByRole('button', { name: /search console/i })
      await user.click(searchBtn)

      expect(screen.getByPlaceholderText(/type a command/i)).toBeInTheDocument()
      expect(screen.getByText('Trinetra Command Palette')).toBeInTheDocument()
    })

    it('opens command palette on pressing slash "/" key', async () => {
      render(<App />)

      fireEvent.keyDown(window, { key: '/' })

      await waitFor(() => {
        expect(screen.getByPlaceholderText(/type a command/i)).toBeInTheDocument()
      })
    })

    it('filters command palette results as user types', async () => {
      const user = userEvent.setup()
      render(<App />)

      fireEvent.keyDown(window, { key: '/' })

      const input = await screen.findByPlaceholderText(/type a command/i)
      await user.type(input, 'Database')

      const palette = screen.getByRole('dialog', { name: /command palette/i })
      expect(within(palette).getByText(/INC-2024-8901/i)).toBeInTheDocument()
      expect(within(palette).queryByText(/Runbooks Catalog/i)).not.toBeInTheDocument()
    })
  })

  describe('Keyboard Shortcuts Helper Modal', () => {
    it('opens shortcuts modal when pressing "?" key', async () => {
      render(<App />)

      fireEvent.keyDown(window, { key: '?' })

      await waitFor(() => {
        expect(screen.getByRole('dialog', { name: /keyboard shortcuts/i })).toBeInTheDocument()
      })

      expect(screen.getByText(/Navigation \(Chords\)/i)).toBeInTheDocument()
      expect(screen.getByText('Go to Incidents')).toBeInTheDocument()
      expect(screen.getByText('Go to Agents')).toBeInTheDocument()
      expect(screen.getByText('Go to Approvals')).toBeInTheDocument()
    })
  })

  describe('Navigation Sequences (GitHub Chords)', () => {
    it('navigates to /agents when pressing "g" then "a"', async () => {
      render(<App />)

      // Press 'g'
      fireEvent.keyDown(window, { key: 'g' })
      // Press 'a'
      fireEvent.keyDown(window, { key: 'a' })

      await waitFor(() => {
        expect(
          screen.getByRole('heading', { level: 2, name: /autonomous agent fleet/i }),
        ).toBeInTheDocument()
      })
    })

    it('navigates to /approvals when pressing "g" then "p"', async () => {
      render(<App />)

      // Press 'g'
      fireEvent.keyDown(window, { key: 'g' })
      // Press 'p'
      fireEvent.keyDown(window, { key: 'p' })

      await waitFor(() => {
        expect(
          screen.getByRole('heading', { level: 2, name: /remediation approval queue/i }),
        ).toBeInTheDocument()
      })
    })
  })
})
