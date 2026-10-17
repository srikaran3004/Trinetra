import type { Meta, StoryObj } from '@storybook/react'
import { SegmentedControl } from './segmented-control'
import { LayoutGrid, List, Table } from 'lucide-react'

const sampleSegments = [
  { id: 'table', label: 'Table View', icon: <Table size={14} strokeWidth={1.5} /> },
  { id: 'cards', label: 'Cards View', icon: <LayoutGrid size={14} strokeWidth={1.5} /> },
  { id: 'list', label: 'List View', icon: <List size={14} strokeWidth={1.5} /> },
]

const meta: Meta<typeof SegmentedControl> = {
  title: 'Inputs/SegmentedControl',
  component: SegmentedControl,
  tags: ['autodocs'],
  args: {
    'aria-label': 'Incident presentation layout',
    options: sampleSegments,
    defaultValue: 'table',
  },
}

export default meta
type Story = StoryObj<typeof SegmentedControl>

export const Default: Story = {}

export const Sizes: Story = {
  render: () => (
    <div className="flex flex-col gap-4 items-start">
      <SegmentedControl
        aria-label="Small presentation view"
        segmentSize="sm"
        options={sampleSegments}
        defaultValue="table"
      />
      <SegmentedControl
        aria-label="Medium presentation view"
        segmentSize="md"
        options={sampleSegments}
        defaultValue="table"
      />
      <SegmentedControl
        aria-label="Large presentation view"
        segmentSize="lg"
        options={sampleSegments}
        defaultValue="table"
      />
    </div>
  ),
}

export const Disabled: Story = {
  args: {
    'aria-label': 'Presentation layout',
    options: sampleSegments,
    defaultValue: 'table',
    isDisabled: true,
  },
}
