import { describe, it, expect } from 'vitest'
import tokensJson from '../../../tokens.json'

/**
 * Calculates WCAG 2.2 relative luminance of a hex color (#RRGGBB).
 */
function getRelativeLuminance(hex: string): number {
  const sanitized = hex.replace('#', '')
  const r = parseInt(sanitized.substring(0, 2), 16) / 255
  const g = parseInt(sanitized.substring(2, 4), 16) / 255
  const b = parseInt(sanitized.substring(4, 6), 16) / 255

  const transform = (c: number) => (c <= 0.04045 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4))

  return 0.2126 * transform(r) + 0.7152 * transform(g) + 0.0722 * transform(b)
}

/**
 * Calculates WCAG 2.2 contrast ratio between two hex colors: (L1 + 0.05) / (L2 + 0.05).
 */
function getContrastRatio(hex1: string, hex2: string): number {
  const lum1 = getRelativeLuminance(hex1)
  const lum2 = getRelativeLuminance(hex2)
  const lighter = Math.max(lum1, lum2)
  const darker = Math.min(lum1, lum2)
  return (lighter + 0.05) / (darker + 0.05)
}

describe('Design Tokens Contrast Verification (WCAG 2.2 Gate)', () => {
  const darkColors = tokensJson.dark.color
  const lightColors = tokensJson.light.color

  describe('Dark Theme Token Contrast', () => {
    const backgrounds = [
      { name: 'canvas', value: darkColors.canvas.value },
      { name: 'surface-raised', value: darkColors['surface-raised'].value },
    ]

    it('asserts body text tokens achieve >= 4.5:1 contrast against dark backgrounds', () => {
      const textTokens = [
        { name: 'text-primary', value: darkColors['text-primary'].value },
        { name: 'text-secondary', value: darkColors['text-secondary'].value },
        { name: 'accent-text', value: darkColors['accent-text'].value },
      ]

      for (const bg of backgrounds) {
        for (const text of textTokens) {
          const ratio = getContrastRatio(text.value, bg.value)
          expect(
            ratio,
            `Dark theme: "${text.name}" (${text.value}) on "${bg.name}" (${bg.value}) ratio ${ratio.toFixed(2)} must be >= 4.5:1`,
          ).toBeGreaterThanOrEqual(4.5)
        }
      }
    })

    it('asserts UI component and status tokens achieve >= 3.0:1 contrast against dark backgrounds', () => {
      const uiTokens = [
        { name: 'text-tertiary', value: darkColors['text-tertiary'].value },
        { name: 'accent', value: darkColors.accent.value },
        { name: 'critical', value: darkColors.critical.value },
        { name: 'high', value: darkColors.high.value },
        { name: 'medium', value: darkColors.medium.value },
        { name: 'low', value: darkColors.low.value },
        { name: 'success', value: darkColors.success.value },
      ]

      for (const bg of backgrounds) {
        for (const ui of uiTokens) {
          const ratio = getContrastRatio(ui.value, bg.value)
          expect(
            ratio,
            `Dark theme: "${ui.name}" (${ui.value}) on "${bg.name}" (${bg.value}) ratio ${ratio.toFixed(2)} must be >= 3.0:1`,
          ).toBeGreaterThanOrEqual(3.0)
        }
      }
    })
  })

  describe('Light Theme Token Contrast', () => {
    const backgrounds = [
      { name: 'canvas', value: lightColors.canvas.value },
      { name: 'surface-raised', value: lightColors['surface-raised'].value },
    ]

    it('asserts body text tokens achieve >= 4.5:1 contrast against light backgrounds', () => {
      const textTokens = [
        { name: 'text-primary', value: lightColors['text-primary'].value },
        { name: 'text-secondary', value: lightColors['text-secondary'].value },
        { name: 'text-tertiary', value: lightColors['text-tertiary'].value },
        { name: 'accent-text', value: lightColors['accent-text'].value },
      ]

      for (const bg of backgrounds) {
        for (const text of textTokens) {
          const ratio = getContrastRatio(text.value, bg.value)
          expect(
            ratio,
            `Light theme: "${text.name}" (${text.value}) on "${bg.name}" (${bg.value}) ratio ${ratio.toFixed(2)} must be >= 4.5:1`,
          ).toBeGreaterThanOrEqual(4.5)
        }
      }
    })

    it('asserts UI component and status tokens achieve >= 3.0:1 contrast against light backgrounds', () => {
      const uiTokens = [
        { name: 'accent', value: lightColors.accent.value },
        { name: 'critical', value: lightColors.critical.value },
        { name: 'high', value: lightColors.high.value },
        { name: 'medium', value: lightColors.medium.value },
        { name: 'low', value: lightColors.low.value },
        { name: 'success', value: lightColors.success.value },
      ]

      for (const bg of backgrounds) {
        for (const ui of uiTokens) {
          const ratio = getContrastRatio(ui.value, bg.value)
          expect(
            ratio,
            `Light theme: "${ui.name}" (${ui.value}) on "${bg.name}" (${bg.value}) ratio ${ratio.toFixed(2)} must be >= 3.0:1`,
          ).toBeGreaterThanOrEqual(3.0)
        }
      }
    })
  })
})
