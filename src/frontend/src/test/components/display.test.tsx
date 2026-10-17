import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Badge } from '@/components/ui/badge'
import { StatusDot } from '@/components/ui/status-dot'
import { Avatar } from '@/components/ui/avatar'
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card'
import { Panel } from '@/components/ui/panel'
import { MetricTile } from '@/components/ui/metric-tile'
import { Countdown } from '@/components/ui/countdown'
import { CodeBlock } from '@/components/ui/code-block'
import { MarkdownView, parseAndSanitizeMarkdown } from '@/components/ui/markdown-view'

describe('Display Components', () => {
  describe('Badge', () => {
    it('renders severity badges with uppercase text and icons', () => {
      render(<Badge severity="critical" />)
      expect(screen.getByText('CRITICAL')).toBeInTheDocument()
    })

    it('renders incident state badges with proper labels', () => {
      render(<Badge incidentState="awaiting_approval" />)
      expect(screen.getByText('Awaiting Approval')).toBeInTheDocument()
    })

    it('renders environment badges', () => {
      render(<Badge env="prod" />)
      expect(screen.getByText('Production')).toBeInTheDocument()
    })
  })

  describe('StatusDot', () => {
    it('renders accessible label for screen readers', () => {
      render(<StatusDot status="critical" label="Critical outage" />)
      expect(screen.getByText('Critical outage')).toBeInTheDocument()
    })

    it('renders pulsing indicator when requested', () => {
      const { container } = render(<StatusDot status="accent" isPulsing label="Active" />)
      expect(container.querySelector('.animate-ping')).toBeInTheDocument()
    })
  })

  describe('Avatar', () => {
    it('renders initials when provided', () => {
      render(<Avatar initials="TR" />)
      expect(screen.getByText('TR')).toBeInTheDocument()
    })

    it('renders agent icon fallback when isAgent is true', () => {
      render(<Avatar isAgent alt="Agent Bot" />)
      expect(screen.getByRole('img', { name: 'Agent Bot' })).toBeInTheDocument()
    })
  })

  describe('Card & Panel', () => {
    it('renders Card hierarchy correctly', () => {
      render(
        <Card>
          <CardHeader>
            <CardTitle>Test Card</CardTitle>
          </CardHeader>
          <CardContent>Body content</CardContent>
        </Card>,
      )
      expect(screen.getByText('Test Card')).toBeInTheDocument()
      expect(screen.getByText('Body content')).toBeInTheDocument()
    })

    it('renders Panel with title and action elements', () => {
      render(
        <Panel title="Logs Snapshot" actions={<button type="button">Refresh</button>}>
          Log text
        </Panel>,
      )
      expect(screen.getByText('Logs Snapshot')).toBeInTheDocument()
      expect(screen.getByText('Refresh')).toBeInTheDocument()
      expect(screen.getByText('Log text')).toBeInTheDocument()
    })
  })

  describe('MetricTile', () => {
    it('renders metric tile with value, unit, and Target badge', () => {
      render(
        <MetricTile
          label="Mean Time to Resolution"
          value="11.4"
          unit="min"
          target="< 15 min"
          status="success"
        />,
      )
      expect(screen.getByText('Mean Time to Resolution')).toBeInTheDocument()
      expect(screen.getByText('11.4')).toBeInTheDocument()
      expect(screen.getByText('min')).toBeInTheDocument()
      expect(screen.getByText('Target')).toBeInTheDocument()
      expect(screen.getByText('< 15 min')).toBeInTheDocument()
    })
  })

  describe('Countdown', () => {
    it('renders formatted minutes when remaining time > 60s', () => {
      render(<Countdown initialSeconds={900} />)
      expect(screen.getByText('15m')).toBeInTheDocument()
    })

    it('renders seconds when remaining time <= 60s per §7.6', () => {
      render(<Countdown initialSeconds={45} />)
      expect(screen.getByText('0:45')).toBeInTheDocument()
    })

    it('triggers onTimeout callback when countdown reaches zero', () => {
      const onTimeout = vi.fn()
      render(<Countdown initialSeconds={0} onTimeout={onTimeout} />)
      expect(screen.getByText('0:00')).toBeInTheDocument()
      expect(onTimeout).toHaveBeenCalled()
    })
  })

  describe('CodeBlock', () => {
    it('renders code snippet and handles copy action', async () => {
      const user = userEvent.setup()
      const writeTextMock = vi.fn().mockResolvedValue(undefined)
      Object.defineProperty(navigator, 'clipboard', {
        value: { writeText: writeTextMock },
        writable: true,
        configurable: true,
      })

      render(<CodeBlock code="echo hello" title="command.sh" />)
      expect(screen.getByText('echo hello')).toBeInTheDocument()
      expect(screen.getByText('command.sh')).toBeInTheDocument()

      const copyBtn = screen.getByRole('button', { name: /copy/i })
      await user.click(copyBtn)
      expect(writeTextMock).toHaveBeenCalledWith('echo hello')
    })
  })

  describe('MarkdownView & Sanitizer Gate', () => {
    it('sanitizes dangerous XSS vectors: scripts, iframes, and onerror attributes', () => {
      const malicious = `
# Title
<script>alert("pwned")</script>
<iframe src="https://evil.com"></iframe>
<img src="invalid.jpg" onerror="alert('xss')" />
[Safe Link](https://trinetra.io)
      `
      const sanitized = parseAndSanitizeMarkdown(malicious)
      expect(sanitized).not.toContain('<script')
      expect(sanitized).not.toContain('<iframe')
      expect(sanitized).not.toContain('onerror')
      expect(sanitized).toContain('Safe Link')
      expect(sanitized).toContain('href="https://trinetra.io"')
      expect(sanitized).toContain('rel="noopener noreferrer"')
    })

    it('renders markdown in DOM safely', () => {
      render(
        <MarkdownView
          content={`
### Runbook Step 1
Execute **safe command**: \`kubectl get pods\`
          `}
        />,
      )
      expect(screen.getByText('Runbook Step 1')).toBeInTheDocument()
      expect(screen.getByText('safe command')).toBeInTheDocument()
      expect(screen.getByText('kubectl get pods')).toBeInTheDocument()
    })
  })
})
