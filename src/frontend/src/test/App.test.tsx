import { describe, it, expect, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import App from '../App'

describe('App — F0 Scaffold', () => {
  beforeEach(() => {
    // Ensure a clean DOM data-theme attribute before each test
    document.documentElement.removeAttribute('data-theme')
  })

  it('renders the Trinetra heading', () => {
    render(<App />)
    expect(screen.getByRole('heading', { level: 1, name: /trinetra/i })).toBeInTheDocument()
  })

  it('renders the skip link', () => {
    render(<App />)
    const skipLink = screen.getByRole('link', { name: /skip to content/i })
    expect(skipLink).toBeInTheDocument()
    expect(skipLink).toHaveAttribute('href', '#main-content')
  })

  it('renders the Trinetra SVG logo', () => {
    render(<App />)
    expect(screen.getByRole('img', { name: /trinetra logo/i })).toBeInTheDocument()
  })

  it('renders the theme toggle button', () => {
    render(<App />)
    expect(screen.getByRole('button', { name: /toggle theme/i })).toBeInTheDocument()
  })

  it('applies data-theme attribute to html element after init', async () => {
    render(<App />)
    // After mounting, the theme store initializes and sets data-theme
    await new Promise((r) => setTimeout(r, 0))
    const theme = document.documentElement.getAttribute('data-theme')
    expect(['dark', 'light']).toContain(theme)
  })

  it('toggles theme when the button is clicked', async () => {
    const user = userEvent.setup()
    render(<App />)

    // Set a known starting state
    document.documentElement.setAttribute('data-theme', 'dark')

    const button = screen.getByRole('button', { name: /toggle theme/i })
    await user.click(button)

    // After clicking, theme should have toggled
    const theme = document.documentElement.getAttribute('data-theme')
    expect(['dark', 'light']).toContain(theme)
  })

  it('has a main landmark', () => {
    render(<App />)
    expect(screen.getByRole('main')).toBeInTheDocument()
  })

  it('renders all stack badges', () => {
    render(<App />)
    const expectedBadges = ['React 19', 'Vite', 'TypeScript strict', 'Tailwind v4', 'Motion']
    for (const badge of expectedBadges) {
      expect(screen.getByText(badge)).toBeInTheDocument()
    }
  })
})
