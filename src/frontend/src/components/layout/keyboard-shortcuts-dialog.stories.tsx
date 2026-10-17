import type { Meta, StoryObj } from '@storybook/react'
import { KeyboardShortcutsDialog } from './keyboard-shortcuts-dialog'

const meta: Meta<typeof KeyboardShortcutsDialog> = {
  title: 'Layout/KeyboardShortcutsDialog',
  component: KeyboardShortcutsDialog,
  parameters: {
    docs: {
      description: {
        component: `
### Keyboard Shortcuts Dialog (§4.9 GitHub pattern)
- **Trigger:** \`?\` key.
- **Features:** Categorized modal displaying navigation chords, search, and general shortcuts.
        `,
      },
    },
  },
}

export default meta
type Story = StoryObj<typeof KeyboardShortcutsDialog>

export const Open: Story = {
  args: {
    isOpen: true,
    onOpenChange: () => {},
  },
}
