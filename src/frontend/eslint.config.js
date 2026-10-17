import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import jsxA11y from 'eslint-plugin-jsx-a11y'
import tseslint from 'typescript-eslint'

export default tseslint.config(
  { ignores: ['dist', 'storybook-static', 'coverage', 'playwright-report'] },
  {
    extends: [js.configs.recommended, ...tseslint.configs.strictTypeChecked],
    files: ['**/*.{ts,tsx}'],
    languageOptions: {
      ecmaVersion: 2022,
      globals: globals.browser,
      parserOptions: {
        project: ['./tsconfig.app.json', './tsconfig.node.json'],
        tsconfigRootDir: import.meta.dirname,
      },
    },
    plugins: {
      'react-hooks': reactHooks,
      'react-refresh': reactRefresh,
      'jsx-a11y': jsxA11y,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      ...jsxA11y.configs.recommended.rules,
      'react-refresh/only-export-components': 'off',
      '@typescript-eslint/restrict-template-expressions': [
        'error',
        { allowNumber: true, allowBoolean: true },
      ],
      '@typescript-eslint/no-deprecated': 'off',

      // === Custom token-enforcement rules ===
      // Reject raw hex colors in inline style objects (must use CSS custom properties)
      'no-restricted-syntax': [
        'error',
        {
          selector:
            'JSXAttribute[name.name="style"] ObjectExpression Property[key.name=/color|background|border/] Literal[value=/^#[0-9a-fA-F]{3,8}$/]',
          message: 'Use CSS custom property tokens (--color-*) instead of raw hex colors.',
        },
        {
          selector:
            'JSXAttribute[name.name="style"] ObjectExpression Property[key.name="borderRadius"] Literal[value=/^[0-9]+px$/]',
          message:
            'Use radius tokens (radius-xs, radius-sm, radius-md, radius-lg, radius-xl, radius-full) instead of raw px values.',
        },
        {
          selector:
            'JSXAttribute[name.name="style"] ObjectExpression Property[key.name="zIndex"] Literal',
          message:
            'Use z-index tokens (z-base, z-sticky, z-dropdown, z-overlay, z-modal, z-toast, z-tooltip) instead of raw z-index numbers.',
        },
      ],

      '@typescript-eslint/no-unused-vars': ['error', { argsIgnorePattern: '^_' }],
    },
  },
  {
    // Relax some rules for test and config files
    files: [
      '**/*.test.{ts,tsx}',
      '**/*.spec.{ts,tsx}',
      'src/test/**',
      'src/mocks/**',
      '*.config.{ts,js}',
      '.storybook/**',
    ],
    rules: {
      '@typescript-eslint/no-explicit-any': 'off',
      '@typescript-eslint/no-unsafe-assignment': 'off',
      '@typescript-eslint/no-unsafe-member-access': 'off',
      '@typescript-eslint/no-unsafe-argument': 'off',
    },
  },
  {
    // Storybook files
    files: ['**/*.stories.{ts,tsx}', '.storybook/**/*.{ts,tsx}'],
    rules: {
      'react-refresh/only-export-components': 'off',
      '@typescript-eslint/no-explicit-any': 'off',
    },
  },
)
