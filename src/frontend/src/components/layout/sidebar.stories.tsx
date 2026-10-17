import type { Meta, StoryObj } from '@storybook/react'
import { MemoryRouter } from 'react-router'
import { Sidebar } from './sidebar'
import { useSidebarStore } from '@/store/sidebar'

const meta: Meta<typeof Sidebar> = {
  title: 'Layout/Sidebar',
  component: Sidebar,
  parameters: {
    docs: {
      description: {
        component: `
### Sidebar Navigation (Stripe & Notion Reference)
- **Width:** 240px expanded, 64px collapsed.
- **Active item:** Marked by background and a 3px accent indicator on the start border, not color alone (§4.6).
- **Notion IA:** Incidents, Agents, Approvals, Runbooks, Settings. User account and sign-out sit in the footer.
- **Accessibility:** Full keyboard navigability, tooltips on collapsed items, accessible labels for screen readers.
        `,
      },
    },
  },
  decorators: [
    (Story) => (
      <MemoryRouter initialEntries={['/incidents']}>
        <div className="h-[600px] flex bg-[var(--color-canvas)]">
          <Story />
        </div>
      </MemoryRouter>
    ),
  ],
}

export default meta
type Story = StoryObj<typeof Sidebar>

export const Expanded: Story = {
  render: () => {
    useSidebarStore.setState({ isCollapsed: false, isMobileOpen: false })
    return <Sidebar />
  },
}

export const Collapsed: Story = {
  render: () => {
    useSidebarStore.setState({ isCollapsed: true, isMobileOpen: false })
    return <Sidebar />
  },
}
