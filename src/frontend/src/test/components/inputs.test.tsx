import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { TextField } from '@/components/ui/text-field'
import { TextArea } from '@/components/ui/text-area'
import { Select } from '@/components/ui/select'
import { Combobox } from '@/components/ui/combobox'
import { Checkbox, CheckboxGroup } from '@/components/ui/checkbox'
import { Switch } from '@/components/ui/switch'
import { RadioGroup, Radio } from '@/components/ui/radio-group'
import { SegmentedControl } from '@/components/ui/segmented-control'
import { DateRange } from '@/components/ui/date-range'

describe('Inputs Components', () => {
  it('renders TextField with label, placeholder, and error message', () => {
    render(
      <TextField
        label="Incident Name"
        placeholder="Enter name"
        errorMessage="Name is required"
        isInvalid
      />,
    )
    expect(screen.getByLabelText(/Incident Name/i)).toBeInTheDocument()
    expect(screen.getByPlaceholderText('Enter name')).toBeInTheDocument()
    expect(screen.getByText('Name is required')).toBeInTheDocument()
  })

  it('renders TextArea with value and rows', async () => {
    const user = userEvent.setup()
    render(<TextArea label="Denial Reason" placeholder="Type reason" rows={4} />)
    const textarea = screen.getByLabelText(/Denial Reason/i)
    expect(textarea).toBeInTheDocument()
    await user.type(textarea, 'Unsafe rollback script')
    expect(textarea).toHaveValue('Unsafe rollback script')
  })

  it('renders Select with options and handles selection', async () => {
    const user = userEvent.setup()
    const onSelectionChange = vi.fn()
    const options = [
      { id: 'critical', label: 'Critical' },
      { id: 'high', label: 'High' },
    ]

    render(
      <Select
        label="Severity"
        options={options}
        onSelectionChange={(key) => {
          onSelectionChange(key)
        }}
      />,
    )

    const trigger = screen.getByRole('button', { name: /Severity/i })
    expect(trigger).toBeInTheDocument()
    await user.click(trigger)

    const option = await screen.findByRole('option', { name: 'Critical' })
    expect(option).toBeInTheDocument()
    await user.click(option)
    expect(onSelectionChange).toHaveBeenCalledWith('critical')
  })

  it('renders Combobox with filter input', () => {
    const options = [
      { id: 'auth', label: 'auth-service' },
      { id: 'payment', label: 'payment-service' },
    ]
    render(<Combobox label="Service" options={options} placeholder="Search service" />)
    expect(screen.getByRole('combobox', { name: /Service/i })).toBeInTheDocument()
  })

  it('toggles Checkbox on user click', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    render(<Checkbox onChange={onChange}>Auto-remediation</Checkbox>)
    const checkbox = screen.getByRole('checkbox', { name: /Auto-remediation/i })
    expect(checkbox).not.toBeChecked()
    await user.click(checkbox)
    expect(onChange).toHaveBeenCalledWith(true)
  })

  it('renders CheckboxGroup with multiple options', () => {
    render(
      <CheckboxGroup label="Sources">
        <Checkbox value="logs">Logs</Checkbox>
        <Checkbox value="metrics">Metrics</Checkbox>
      </CheckboxGroup>,
    )
    expect(screen.getByRole('group', { name: /Sources/i })).toBeInTheDocument()
    expect(screen.getByRole('checkbox', { name: /Logs/i })).toBeInTheDocument()
    expect(screen.getByRole('checkbox', { name: /Metrics/i })).toBeInTheDocument()
  })

  it('toggles Switch state on click', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    render(<Switch onChange={onChange}>Reduced Motion</Switch>)
    const toggle = screen.getByRole('switch', { name: /Reduced Motion/i })
    expect(toggle).not.toBeChecked()
    await user.click(toggle)
    expect(onChange).toHaveBeenCalledWith(true)
  })

  it('renders RadioGroup and selects radio', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    render(
      <RadioGroup label="Status" onChange={onChange} defaultValue="active">
        <Radio value="active">Active</Radio>
        <Radio value="resolved">Resolved</Radio>
      </RadioGroup>,
    )
    const activeRadio = screen.getByRole('radio', { name: /Active/i })
    const resolvedRadio = screen.getByRole('radio', { name: /Resolved/i })
    expect(activeRadio).toBeChecked()
    expect(resolvedRadio).not.toBeChecked()

    await user.click(resolvedRadio)
    expect(onChange).toHaveBeenCalledWith('resolved')
  })

  it('renders SegmentedControl and allows selection', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    const options = [
      { id: 'table', label: 'Table' },
      { id: 'cards', label: 'Cards' },
    ]
    render(
      <SegmentedControl
        aria-label="View format"
        options={options}
        defaultValue="table"
        onChange={onChange}
      />,
    )
    const cardsRadio = screen.getByRole('radio', { name: /Cards/i })
    await user.click(cardsRadio)
    expect(onChange).toHaveBeenCalledWith('cards')
  })

  it('renders DateRange with preset selection', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    render(
      <DateRange
        defaultValue={{ preset: '30d', start: '2026-09-17', end: '2026-10-17' }}
        onChange={onChange}
      />,
    )
    const btn7d = screen.getByRole('button', { name: '7 days' })
    await user.click(btn7d)
    expect(onChange).toHaveBeenCalledWith(
      expect.objectContaining({
        preset: '7d',
      }),
    )
  })
})
