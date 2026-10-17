import type { Meta, StoryObj } from '@storybook/react'
import { MetricTile } from './metric-tile'
import { Timer, Zap, CheckCircle2 } from 'lucide-react'

const meta: Meta<typeof MetricTile> = {
  title: 'Display/MetricTile',
  component: MetricTile,
  tags: ['autodocs'],
}

export default meta
type Story = StoryObj<typeof MetricTile>

export const MTTRTarget: Story = {
  args: {
    label: 'Mean Time to Resolution (MTTR)',
    value: '11.4',
    unit: 'min',
    target: '< 15 min',
    delta: { value: '2.1m faster', isPositive: true },
    status: 'success',
    icon: <Timer size={16} strokeWidth={1.5} />,
  },
}

export const MTTATarget: Story = {
  args: {
    label: 'Mean Time to Acknowledge (MTTA)',
    value: '42',
    unit: 'sec',
    target: '< 2 min',
    delta: { value: '18s faster', isPositive: true },
    status: 'success',
    icon: <Zap size={16} strokeWidth={1.5} />,
  },
}

export const AutoResolutionTarget: Story = {
  args: {
    label: 'Autonomous Resolution Rate',
    value: '68.5',
    unit: '%',
    target: '> 60%',
    delta: { value: '+4.2% vs last 30d', isPositive: true },
    status: 'success',
    icon: <CheckCircle2 size={16} strokeWidth={1.5} />,
  },
}

export const CriticalStatus: Story = {
  args: {
    label: 'Active Critical Incidents',
    value: '3',
    unit: 'active',
    status: 'critical',
    delta: { value: '+2 new', isPositive: false },
  },
}
