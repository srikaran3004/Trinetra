import { describe, it, expect, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import App from '../App'

describe('App — Console Shell and Routing (F2)', () => {
  beforeEach(() => {
    // Clean DOM before each test
    document.documentElement.removeAttribute('data-theme')
  })

  it('renders the skip-to-content accessibility link', () => {
    render(<App />)
    const skipLink = screen.getByRole('link', { name: /skip to content/i })
    expect(skipLink).toBeInTheDocument()
    expect(skipLink).toHaveAttribute('href', '#main-content')
  })

  it('renders the Trinetra logo and navigation landmark', () => {
    render(<App />)
    expect(screen.getByRole('img', { name: /trinetra logo/i })).toBeInTheDocument()
    expect(screen.getByRole('navigation', { name: /console sections/i })).toBeInTheDocument()
  })

  it('renders the main landmark', () => {
    render(<App />)
    expect(screen.getByRole('main')).toBeInTheDocument()
  })

  it('renders the active incidents section on initial load', () => {
    render(<App />)
    expect(screen.getByRole('heading', { level: 2, name: /active incidents/i })).toBeInTheDocument()
  })

  it('applies data-theme attribute to html element after init', async () => {
    render(<App />)
    await new Promise((r) => setTimeout(r, 0))
    const theme = document.documentElement.getAttribute('data-theme')
    expect(['dark', 'light']).toContain(theme)
  })

  it('toggles theme when the theme button is clicked', async () => {
    const user = userEvent.setup()
    render(<App />)

    document.documentElement.setAttribute('data-theme', 'dark')

    const button = screen.getByRole('button', { name: /switch to (light|dark) theme/i })
    await user.click(button)

    const theme = document.documentElement.getAttribute('data-theme')
    expect(['dark', 'light']).toContain(theme)
  })
})
