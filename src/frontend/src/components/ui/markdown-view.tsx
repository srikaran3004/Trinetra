import { useMemo } from 'react'
import DOMPurify from 'dompurify'
import { cn } from '@/lib/cn'

export interface MarkdownViewProps {
  content: string
  className?: string
}

/**
 * Lightweight safe markdown to HTML parser sanitized with DOMPurify.
 * Supports headings, bold, italics, code blocks, lists, blockquotes, and links.
 */
export function parseAndSanitizeMarkdown(markdown: string): string {
  if (!markdown) return ''

  // 1. Normalize line breaks
  let html = markdown.replace(/\r\n/g, '\n')

  // 2. Multiline code blocks
  html = html.replace(/```([a-z0-9_-]*)\n([\s\S]*?)```/gm, (_match, _lang, code: string) => {
    const escaped = code
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
    return `<pre class="p-3 my-2 bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)] rounded-[var(--radius-xs)] overflow-x-auto font-mono text-xs text-[var(--color-text-primary)]"><code>${escaped}</code></pre>`
  })

  // 3. Inline code
  html = html.replace(/`([^`\n]+)`/g, (_match, code: string) => {
    const escaped = code.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
    return `<code class="px-1.5 py-0.5 bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)] rounded-[var(--radius-xs)] font-mono text-xs text-[var(--color-accent-text)]">${escaped}</code>`
  })

  // 4. Headings
  html = html.replace(
    /^### (.*$)/gim,
    '<h3 class="text-sm font-semibold text-[var(--color-text-primary)] mt-3 mb-1">$1</h3>',
  )
  html = html.replace(
    /^## (.*$)/gim,
    '<h2 class="text-base font-semibold text-[var(--color-text-primary)] mt-4 mb-2">$1</h2>',
  )
  html = html.replace(
    /^# (.*$)/gim,
    '<h1 class="text-lg font-bold text-[var(--color-text-primary)] mt-5 mb-2">$1</h1>',
  )

  // 5. Blockquotes
  html = html.replace(
    /^> (.*$)/gim,
    '<blockquote class="border-s-2 border-[var(--color-accent)] ps-3 my-2 text-xs text-[var(--color-text-secondary)] italic">$1</blockquote>',
  )

  // 6. Bold & Italic
  html = html.replace(
    /\*\*([^*]+)\*\*/g,
    '<strong class="font-semibold text-[var(--color-text-primary)]">$1</strong>',
  )
  html = html.replace(/\*([^*]+)\*/g, '<em class="italic">$1</em>')

  // 7. Unordered lists
  html = html.replace(
    /^- (.*$)/gim,
    '<li class="ms-4 list-disc text-xs text-[var(--color-text-secondary)] my-0.5">$1</li>',
  )

  // 8. Safe links (forces noopener noreferrer and opens in new tab)
  html = html.replace(
    /\[([^\]]+)\]\(([^)]+)\)/g,
    '<a href="$2" target="_blank" rel="noopener noreferrer" class="text-[var(--color-accent)] hover:underline font-medium">$1</a>',
  )

  // 9. Paragraphs (lines separated by double newlines)
  const paragraphs = html.split('\n\n')
  const wrapped = paragraphs
    .map((para) => {
      const trimmed = para.trim()
      if (!trimmed) return ''
      if (
        trimmed.startsWith('<h') ||
        trimmed.startsWith('<pre') ||
        trimmed.startsWith('<blockquote') ||
        trimmed.startsWith('<li')
      ) {
        return trimmed
      }
      return `<p class="text-xs leading-relaxed text-[var(--color-text-secondary)] my-1.5">${trimmed.replace(/\n/g, '<br/>')}</p>`
    })
    .join('\n')

  // 10. Strict DOMPurify sanitization
  return DOMPurify.sanitize(wrapped, {
    ALLOWED_TAGS: [
      'h1',
      'h2',
      'h3',
      'p',
      'strong',
      'em',
      'code',
      'pre',
      'blockquote',
      'ul',
      'ol',
      'li',
      'a',
      'br',
      'span',
      'div',
    ],
    ALLOWED_ATTR: ['href', 'target', 'rel', 'class'],
  })
}

export function MarkdownView({ content, className }: MarkdownViewProps) {
  const sanitizedHtml = useMemo(() => parseAndSanitizeMarkdown(content), [content])

  return (
    <div
      className={cn(
        'text-start text-xs leading-relaxed text-[var(--color-text-secondary)]',
        className,
      )}
      dangerouslySetInnerHTML={{ __html: sanitizedHtml }}
    />
  )
}
