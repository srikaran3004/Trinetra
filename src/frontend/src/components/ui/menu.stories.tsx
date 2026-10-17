import type { Meta, StoryObj } from '@storybook/react'
import { Menu } from './menu'
import { Button } from './button'
import { MoreVertical, Copy, RotateCcw, AlertTriangle, Trash2 } from 'lucide-react'

const sampleMenuItems = [
  {
    id: 'copy',
    label: 'Copy Incident ID',
    icon: <Copy size={14} strokeWidth={1.5} />,
    shortcut: '⌘C',
  },
  {
    id: 'retry',
    label: 'Re-run Auto-Diagnostics',
    icon: <RotateCcw size={14} strokeWidth={1.5} />,
    shortcut: '⌘R',
  },
  { id: 'escalate', label: 'Escalate to P1', icon: <AlertTriangle size={14} strokeWidth={1.5} /> },
  {
    id: 'abort',
    label: 'Abort Incident',
    icon: <Trash2 size={14} strokeWidth={1.5} />,
    isDanger: true,
  },
]

const meta: Meta<typeof Menu> = {
  title: 'Overlays/Menu',
  component: Menu,
  tags: ['autodocs'],
}

export default meta
type Story = StoryObj<typeof Menu>

export const Default: Story = {
  render: () => (
    <Menu
      trigger={
        <Button size="sm" variant="ghost">
          <MoreVertical size={14} strokeWidth={1.5} />
          <span>Actions</span>
        </Button>
      }
      items={sampleMenuItems}
    />
  ),
}
