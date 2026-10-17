import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Button } from '@/components/ui/button'
import { IconButton } from '@/components/ui/icon-button'
import { Link } from '@/components/ui/link'
import { KeyboardShortcut } from '@/components/ui/keyboard-shortcut'
import { RefreshCw } from 'lucide-react'

describe('Actions Components', () => {
  describe('Button', () => {
    it('renders with children and handles click', async () => {
      const user = userEvent.setup()
      const onClick = vi.fn()
      render(<Button onPress={onClick}>Approve action</Button>)

      const btn = screen.getByRole('button', { name: /approve action/i })
      expect(btn).toBeInTheDocument()
      await user.click(btn)
      expect(onClick).toHaveBeenCalledTimes(1)
    })

    it('renders loading spinner and disables button when isLoading is true', () => {
      render(
        <Button isLoading loadingText="Executing...">
          Execute
        </Button>,
      )
      const btn = screen.getByRole('button')
      expect(btn).toBeDisabled()
      expect(screen.getByText('Executing...')).toBeInTheDocument()
    })

    it('applies danger variant classes correctly', () => {
      render(<Button variant="danger">Abort</Button>)
      const btn = screen.getByRole('button', { name: /abort/i })
      expect(btn.className).toContain('bg-[var(--color-critical)]')
    })
  })

  describe('IconButton', () => {
    it('requires and renders accessible name via aria-label', () => {
      render(<IconButton aria-label="Refresh incidents" icon={<RefreshCw size={16} />} />)
      const btn = screen.getByRole('button', { name: /refresh incidents/i })
      expect(btn).toBeInTheDocument()
    })

    it('disables button when isDisabled is true', () => {
      render(<IconButton aria-label="Close modal" icon={<RefreshCw size={16} />} isDisabled />)
      expect(screen.getByRole('button', { name: /close modal/i })).toBeDisabled()
    })
  })

  describe('Link', () => {
    it('renders link with target _blank when isExternal is true', () => {
      render(
        <Link href="https://example.com" isExternal>
          External Docs
        </Link>,
      )
      const link = screen.getByRole('link', { name: /external docs/i })
      expect(link).toHaveAttribute('target', '_blank')
      expect(link).toHaveAttribute('rel', 'noopener noreferrer')
    })
  })

  describe('KeyboardShortcut', () => {
    it('renders each key in a semantic kbd tag', () => {
      render(<KeyboardShortcut keys={['⌘', 'K']} />)
      const kbdElements = document.querySelectorAll('kbd')
      expect(kbdElements).toHaveLength(2)
      expect(kbdElements[0]?.textContent).toBe('⌘')
      expect(kbdElements[1]?.textContent).toBe('K')
    })
  })
})
