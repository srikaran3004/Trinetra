import { describe, it, expect, vi } from 'vitest'
import { render, screen, act } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Tooltip } from '@/components/ui/tooltip'
import { Popover } from '@/components/ui/popover'
import { Menu } from '@/components/ui/menu'
import { Dialog } from '@/components/ui/dialog'
import { Skeleton } from '@/components/ui/skeleton'
import { Spinner } from '@/components/ui/spinner'
import { InlineAlert } from '@/components/ui/inline-alert'
import { EmptyState } from '@/components/ui/empty-state'
import { ErrorState } from '@/components/ui/error-state'
import { ProgressBar } from '@/components/ui/progress-bar'
import { showToast } from '@/components/ui/toast'

import { Button } from '@/components/ui/button'

describe('Overlays and Feedback Components', () => {
  describe('Tooltip', () => {
    it('renders trigger element', () => {
      render(
        <Tooltip content="Tooltip info">
          <Button>Trigger Button</Button>
        </Tooltip>,
      )
      expect(screen.getByRole('button', { name: 'Trigger Button' })).toBeInTheDocument()
    })
  })

  describe('Popover', () => {
    it('renders trigger and opens popover content on click', async () => {
      const user = userEvent.setup()
      render(
        <Popover trigger={<Button>Open Popover</Button>} title="Cluster Telemetry">
          <p>Popover body content</p>
        </Popover>,
      )
      const trigger = screen.getByRole('button', { name: 'Open Popover' })
      expect(trigger).toBeInTheDocument()
      await user.click(trigger)
      expect(await screen.findByText('Cluster Telemetry')).toBeInTheDocument()
      expect(screen.getByText('Popover body content')).toBeInTheDocument()
    })
  })

  describe('Menu', () => {
    it('renders menu trigger and selects item on click', async () => {
      const user = userEvent.setup()
      const onAction = vi.fn()
      const items = [
        { id: 'copy', label: 'Copy ID' },
        { id: 'retry', label: 'Retry Action' },
      ]
      render(<Menu trigger={<Button>Menu Options</Button>} items={items} onAction={onAction} />)
      const trigger = screen.getByRole('button', { name: 'Menu Options' })
      await user.click(trigger)
      const copyItem = await screen.findByRole('menuitem', { name: 'Copy ID' })
      await user.click(copyItem)
      expect(onAction).toHaveBeenCalledWith('copy', undefined)
    })
  })

  describe('Dialog with High-Risk Typed Confirmation (§7.6)', () => {
    it('disables confirm button until exact confirmation phrase is entered', async () => {
      const user = userEvent.setup()
      const onConfirm = vi.fn()
      render(
        <Dialog isOpen title="High Risk Action" confirmPhrase="INC-9999" onConfirm={onConfirm} />,
      )

      expect(screen.getByText('High Risk Action')).toBeInTheDocument()
      const confirmBtn = screen.getByRole('button', { name: 'Confirm' })
      expect(confirmBtn).toBeDisabled()

      // Type partial or wrong phrase
      const input = screen.getByRole('textbox', { name: 'Confirmation phrase' })
      await user.type(input, 'INC-1111')
      expect(confirmBtn).toBeDisabled()

      // Type exact phrase
      await user.clear(input)
      await user.type(input, 'INC-9999')
      expect(confirmBtn).not.toBeDisabled()

      await user.click(confirmBtn)
      expect(onConfirm).toHaveBeenCalled()
    })
  })

  describe('Toast', () => {
    it('exposes showToast methods', () => {
      expect(typeof showToast.success).toBe('function')
      expect(typeof showToast.error).toBe('function')
      expect(typeof showToast.warning).toBe('function')
      expect(typeof showToast.info).toBe('function')
    })
  })

  describe('Skeleton', () => {
    it('renders with variants', () => {
      const { container } = render(
        <div>
          <Skeleton variant="circle" />
          <Skeleton variant="text" />
          <Skeleton variant="rect" />
        </div>,
      )
      expect(container.querySelector('.rounded-\\[var\\(--radius-full\\)\\]')).toBeInTheDocument()
      expect(container.querySelector('.rounded-\\[var\\(--radius-xs\\)\\]')).toBeInTheDocument()
      expect(container.querySelector('.rounded-\\[var\\(--radius-sm\\)\\]')).toBeInTheDocument()
    })
  })

  describe('Spinner with Timeout Safety Path (§13)', () => {
    it('renders spinner with accessible role', () => {
      render(<Spinner label="Processing..." />)
      expect(screen.getByRole('status', { name: 'Processing...' })).toBeInTheDocument()
    })

    it('triggers timeout callback and error message when timeout expires', () => {
      vi.useFakeTimers()
      const onTimeout = vi.fn()
      render(
        <Spinner timeoutMs={1000} onTimeout={onTimeout} timeoutMessage="Operation timed out" />,
      )
      expect(screen.getByRole('status')).toBeInTheDocument()

      act(() => {
        vi.advanceTimersByTime(1100)
      })

      expect(onTimeout).toHaveBeenCalled()
      expect(screen.getByRole('alert')).toHaveTextContent('Operation timed out')
      vi.useRealTimers()
    })
  })

  describe('InlineAlert', () => {
    it('renders alert message and handles dismiss', async () => {
      const user = userEvent.setup()
      const onDismiss = vi.fn()
      render(
        <InlineAlert variant="critical" title="Rollback Triggered" onDismiss={onDismiss}>
          Container crashed
        </InlineAlert>,
      )
      expect(screen.getByRole('alert')).toBeInTheDocument()
      expect(screen.getByText('Rollback Triggered')).toBeInTheDocument()
      expect(screen.getByText('Container crashed')).toBeInTheDocument()

      const dismissBtn = screen.getByRole('button', { name: 'Dismiss alert' })
      await user.click(dismissBtn)
      expect(onDismiss).toHaveBeenCalled()
    })
  })

  describe('EmptyState & ErrorState', () => {
    it('renders EmptyState with title and action', () => {
      render(
        <EmptyState
          title="No Active Incidents"
          description="Everything is healthy"
          action={<button type="button">Refresh</button>}
        />,
      )
      expect(screen.getByText('No Active Incidents')).toBeInTheDocument()
      expect(screen.getByText('Everything is healthy')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Refresh' })).toBeInTheDocument()
    })

    it('renders ErrorState with retry button and technical details', async () => {
      const user = userEvent.setup()
      const onRetry = vi.fn()
      render(
        <ErrorState
          title="Service Error"
          message="Failed to connect"
          details="Status 500: Database timeout"
          onRetry={onRetry}
        />,
      )
      expect(screen.getByText('Service Error')).toBeInTheDocument()
      expect(screen.getByText('Failed to connect')).toBeInTheDocument()

      const retryBtn = screen.getByRole('button', { name: /try again/i })
      await user.click(retryBtn)
      expect(onRetry).toHaveBeenCalled()

      // Expand technical details
      const detailsToggle = screen.getByRole('button', { name: /technical details/i })
      await user.click(detailsToggle)
      expect(screen.getByText(/Status 500: Database timeout/i)).toBeInTheDocument()
    })
  })

  describe('ProgressBar', () => {
    it('renders progress bar with label and value', () => {
      render(<ProgressBar label="Analysis" value={75} showValue />)
      expect(screen.getByText('Analysis')).toBeInTheDocument()
      expect(screen.getByText('75%')).toBeInTheDocument()
      const progressbar = screen.getByRole('progressbar')
      expect(progressbar).toHaveAttribute('aria-valuenow', '75')
    })
  })
})
