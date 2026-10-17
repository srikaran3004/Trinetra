import type { Meta, StoryObj } from '@storybook/react'
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
  TableCaption,
} from './table'
import { Badge } from './badge'

const meta: Meta<typeof Table> = {
  title: 'Data/Table',
  component: Table,
  tags: ['autodocs'],
}

export default meta
type Story = StoryObj<typeof Table>

export const Default: Story = {
  render: () => (
    <div className="p-4 max-w-4xl">
      <Table>
        <TableCaption>Active autonomous incident investigations.</TableCaption>
        <TableHeader>
          <TableRow>
            <TableHead>Severity</TableHead>
            <TableHead>Incident</TableHead>
            <TableHead>Service</TableHead>
            <TableHead>Status</TableHead>
            <TableHead numeric>Age</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          <TableRow selected>
            <TableCell>
              <Badge severity="critical" size="sm" />
            </TableCell>
            <TableCell>
              <div className="font-medium">Database connection pool exhaustion</div>
              <div className="font-mono text-xs text-[var(--color-text-tertiary)]">
                INC-2024-8901
              </div>
            </TableCell>
            <TableCell>payment-db-primary</TableCell>
            <TableCell>
              <Badge incidentState="awaiting_approval" size="sm" />
            </TableCell>
            <TableCell numeric>3m ago</TableCell>
          </TableRow>
          <TableRow>
            <TableCell>
              <Badge severity="high" size="sm" />
            </TableCell>
            <TableCell>
              <div className="font-medium">Payment gateway timeout cascade</div>
              <div className="font-mono text-xs text-[var(--color-text-tertiary)]">
                INC-2024-8902
              </div>
            </TableCell>
            <TableCell>checkout-api</TableCell>
            <TableCell>
              <Badge incidentState="escalated" size="sm" />
            </TableCell>
            <TableCell numeric>14m ago</TableCell>
          </TableRow>
          <TableRow>
            <TableCell>
              <Badge severity="medium" size="sm" />
            </TableCell>
            <TableCell>
              <div className="font-medium">Memory leak in worker nodes</div>
              <div className="font-mono text-xs text-[var(--color-text-tertiary)]">
                INC-2024-8899
              </div>
            </TableCell>
            <TableCell>order-processor</TableCell>
            <TableCell>
              <Badge incidentState="resolved" size="sm" />
            </TableCell>
            <TableCell numeric>1h ago</TableCell>
          </TableRow>
        </TableBody>
      </Table>
    </div>
  ),
}
