import type { Meta, StoryObj } from '@storybook/react'
import { MemoryRouter } from 'react-router'
import { CommandPalette } from './command-palette'

const meta: Meta<typeof CommandPalette> = {
  title: 'Layout/CommandPalette',
  component: CommandPalette,
  parameters: {
    docs: {
      description: {
        component: `
### Command Palette (§4.9 & Decision D-24)
- **Trigger:** \`/\` or \`Ctrl+K\` / \`Cmd+K\`.
- **Features:** Instant fuzzy filtering across navigation, seeded incidents, and console actions.
- **Keyboard navigation:** ArrowUp, ArrowDown, Enter, Esc.
        `,
      },
    },
  },
  decorators: [
    (Story) => (
      <MemoryRouter initialEntries={['/incidents']}>
        <div className="h-[500px] w-full bg-[var(--color-canvas)] p-8">
          <Story />
        </div>
      </MemoryRouter>
    ),
  ],
}

export default meta
type Story = StoryObj<typeof CommandPalette>

export const Open: Story = {
  args: {
    isOpen: true,
    onOpenChange: () => {},
    onOpenShortcuts: () => {},
  },
}
