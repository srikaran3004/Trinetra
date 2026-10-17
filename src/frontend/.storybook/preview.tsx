import type { Preview } from '@storybook/react'
import '../src/styles/tokens.css'

/**
 * Storybook preview configuration.
 * - Imports the token CSS so stories use the full design system.
 * - Provides dark/light theme switching via the toolbar.
 * - Sets global a11y parameters.
 */
const preview: Preview = {
  parameters: {
    controls: {
      matchers: {
        color: /(background|color)$/i,
        date: /date$/i,
      },
    },
    a11y: {
      // Axe rules — zero serious/critical violations required per brief §11
      config: {
        rules: [
          {
            // Ensure all interactive elements have accessible names
            id: 'button-name',
            enabled: true,
          },
        ],
      },
    },
    backgrounds: {
      // Map Storybook backgrounds to our themes
      default: 'dark',
      values: [
        { name: 'dark', value: '#08090d' },
        { name: 'light', value: '#fafafb' },
      ],
    },
  },
  globalTypes: {
    theme: {
      description: 'Global theme for components',
      defaultValue: 'dark',
      toolbar: {
        title: 'Theme',
        icon: 'circlehollow',
        items: ['dark', 'light'],
        dynamicTitle: true,
      },
    },
  },
  decorators: [
    (Story, context) => {
      const theme = (context.globals['theme'] as string | undefined) ?? 'dark'
      document.documentElement.setAttribute('data-theme', theme)
      return <Story />
    },
  ],
}

export default preview
