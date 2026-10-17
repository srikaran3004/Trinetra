import type { Meta, StoryObj } from '@storybook/react'
import { MemoryRouter } from 'react-router'
import { Topbar } from './topbar'

const meta: Meta<typeof Topbar> = {
  title: 'Layout/Topbar',
  component: Topbar,
  parameters: {
    docs: {
      description: {
        component: `
### Topbar (Console Header)
- **Features:** Breadcrumbs, demo data badge, search trigger, SignalR connection status, shortcuts helper trigger, and theme toggle.
- **Accessibility:** Interactive elements are keyboard reachable with accessible tooltips and ARIA roles.
        `,
      },
    },
  },
  decorators: [
    (Story) => (
      <MemoryRouter initialEntries={['/incidents']}>
        <div className="w-full bg-[var(--color-canvas)] p-4">
          <Story />
        </div>
      </MemoryRouter>
    ),
  ],
}

export default meta
type Story = StoryObj<typeof Topbar>

export const Connected: Story = {
  args: {
    isConnected: true,
    onOpenSearch: () => {},
    onOpenShortcuts: () => {},
  },
}

export const Disconnected: Story = {
  args: {
    isConnected: false,
    onOpenSearch: () => {},
    onOpenShortcuts: () => {},
  },
}
