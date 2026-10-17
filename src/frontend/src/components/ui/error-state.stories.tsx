import type { Meta, StoryObj } from '@storybook/react'
import { ErrorState } from './error-state'

const meta: Meta<typeof ErrorState> = {
  title: 'Feedback/ErrorState',
  component: ErrorState,
  tags: ['autodocs'],
}

export default meta
type Story = StoryObj<typeof ErrorState>

export const Default: Story = {
  args: {
    title: 'Connection to SignalR Hub Lost',
    message: 'Unable to stream live incident updates from ASP.NET Core backend at /hubs/incidents.',
    details:
      'Error: WebSocket failed to connect to ws://localhost:5000/hubs/incidents. StatusCode: 503 Service Unavailable.',
    onRetry: () => {},
  },
}
