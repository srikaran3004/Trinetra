import type { Meta, StoryObj } from '@storybook/react'
import { MemoryRouter } from 'react-router'
import { LoginPage } from './login-page'
import { AuthProvider } from '@/auth/auth-context'

const meta: Meta<typeof LoginPage> = {
  title: 'Pages/LoginPage',
  component: LoginPage,
  parameters: {
    docs: {
      description: {
        component: `
### Login Screen (Brief §10 & Phase F2-2)
- **Features:** Email and password fields with validation, quick-fill demo roles (SRE Lead, Operator, Viewer), and in-memory JWT authentication.
- **Security:** Access tokens are strictly kept in memory and never stored in localStorage.
        `,
      },
    },
  },
  decorators: [
    (Story) => (
      <AuthProvider initialUser={null} initialToken={null}>
        <MemoryRouter initialEntries={['/login']}>
          <Story />
        </MemoryRouter>
      </AuthProvider>
    ),
  ],
}

export default meta
type Story = StoryObj<typeof LoginPage>

export const Default: Story = {}
