/**
 * cn — Class name utility combining clsx + tailwind-merge.
 * Use for conditional classes and merging Tailwind utilities.
 *
 * @example
 *   cn('px-4 py-2', isActive && 'bg-[--color-accent]')
 */
import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}
