import type { Meta, StoryObj } from '@storybook/react'
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from './card'
import { Button } from './button'

const meta: Meta<typeof Card> = {
  title: 'Display/Card',
  component: Card,
  tags: ['autodocs'],
}

export default meta
type Story = StoryObj<typeof Card>

export const Default: Story = {
  render: () => (
    <Card className="max-w-md">
      <CardHeader>
        <CardTitle>Root Cause Evidence Chain</CardTitle>
        <CardDescription>Multi-modal correlation from Elasticsearch & Prometheus</CardDescription>
      </CardHeader>
      <CardContent>
        <p className="text-xs text-[var(--color-text-secondary)] leading-relaxed">
          Database connection pool saturation triggered cascading HTTP 500 timeouts in auth-service.
          Confidence score calculated at 94%.
        </p>
      </CardContent>
      <CardFooter>
        <span className="text-xs text-[var(--color-text-tertiary)]">Evaluated 3 minutes ago</span>
        <Button size="sm" variant="secondary">
          View Snapshot
        </Button>
      </CardFooter>
    </Card>
  ),
}
