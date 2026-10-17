import type { Meta, StoryObj } from '@storybook/react'
import { Toaster, showToast } from './toast'
import { Button } from './button'

const meta: Meta<typeof Toaster> = {
  title: 'Overlays/Toast',
  component: Toaster,
  tags: ['autodocs'],
}

export default meta
type Story = StoryObj<typeof Toaster>

export const TriggerToasts: Story = {
  render: () => (
    <div className="flex flex-wrap gap-2">
      <Toaster />
      <Button
        size="sm"
        onPress={() => {
          showToast.success('Remediation succeeded', 'Pods rolled back cleanly')
        }}
      >
        Success Toast
      </Button>
      <Button
        size="sm"
        variant="danger"
        onPress={() => {
          showToast.error('Guardrail blocked command', 'Command "rm -rf" intercepted and escalated')
        }}
      >
        Critical Toast
      </Button>
      <Button
        size="sm"
        variant="secondary"
        onPress={() => {
          showToast.warning('Timeout warning', '15-minute approval window expires in 2 minutes')
        }}
      >
        Warning Toast
      </Button>
      <Button
        size="sm"
        variant="ghost"
        onPress={() => {
          showToast.info('Agent update', 'Log Analyzer discovered 4 anomalous stack traces')
        }}
      >
        Info Toast
      </Button>
    </div>
  ),
}
