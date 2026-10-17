import { useState } from 'react'
import { Copy, Check, WrapText } from 'lucide-react'
import { cn } from '@/lib/cn'

export interface CodeBlockProps {
  code: string
  language?: string
  title?: string
  showLineNumbers?: boolean
  initialWrapLines?: boolean
  className?: string
}

export function CodeBlock({
  code,
  language,
  title,
  showLineNumbers = false,
  initialWrapLines = false,
  className,
}: CodeBlockProps) {
  const [copied, setCopied] = useState(false)
  const [wrapLines, setWrapLines] = useState(initialWrapLines)

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(code)
      setCopied(true)
      setTimeout(() => {
        setCopied(false)
      }, 2000)
    } catch {
      // Fallback
    }
  }

  const lines = code.trim().split('\n')

  return (
    <div
      className={cn(
        'group relative flex flex-col w-full bg-[var(--color-canvas)] border border-[var(--color-border-subtle)] rounded-[var(--radius-sm)] overflow-hidden',
        className,
      )}
    >
      {(title || language) && (
        <div className="flex items-center justify-between px-3 py-1.5 bg-[var(--color-surface-hover)] border-b border-[var(--color-border-subtle)] text-xs text-[var(--color-text-tertiary)] font-mono">
          <span>{title || language}</span>
          <div className="flex items-center gap-1">
            <button
              type="button"
              onClick={() => {
                setWrapLines((prev) => !prev)
              }}
              title="Toggle line wrapping"
              aria-label="Toggle line wrapping"
              className={cn(
                'p-1 rounded-[var(--radius-xs)] hover:bg-[var(--color-surface-pressed)] transition-colors duration-120 cursor-pointer',
                wrapLines && 'text-[var(--color-accent-text)] bg-[var(--color-surface-pressed)]',
              )}
            >
              <WrapText size={12} strokeWidth={1.5} />
            </button>
            <button
              type="button"
              onClick={() => {
                void handleCopy()
              }}
              aria-label={copied ? 'Copied code to clipboard' : 'Copy code to clipboard'}
              className="inline-flex items-center gap-1 px-2 py-0.5 rounded-[var(--radius-xs)] hover:bg-[var(--color-surface-pressed)] transition-colors duration-120 cursor-pointer text-xs"
            >
              {copied ? (
                <>
                  <Check size={12} strokeWidth={1.5} className="text-[var(--color-success)]" />
                  <span className="text-[var(--color-success)]">Copied</span>
                </>
              ) : (
                <>
                  <Copy size={12} strokeWidth={1.5} />
                  <span>Copy</span>
                </>
              )}
            </button>
          </div>
        </div>
      )}

      {!(title || language) && (
        <button
          type="button"
          onClick={() => {
            void handleCopy()
          }}
          aria-label={copied ? 'Copied code to clipboard' : 'Copy code to clipboard'}
          className="absolute top-2 end-2 z-10 inline-flex items-center gap-1 px-2 py-1 bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)] rounded-[var(--radius-xs)] text-xs text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:border-[var(--color-border-strong)] transition-colors duration-120 cursor-pointer opacity-80 group-hover:opacity-100 shadow-sm"
        >
          {copied ? (
            <>
              <Check size={12} strokeWidth={1.5} className="text-[var(--color-success)]" />
              <span className="text-[var(--color-success)]">Copied</span>
            </>
          ) : (
            <>
              <Copy size={12} strokeWidth={1.5} />
              <span>Copy</span>
            </>
          )}
        </button>
      )}

      <pre
        className={cn(
          'p-3 font-mono text-xs leading-relaxed text-[var(--color-text-primary)] font-normal',
          wrapLines ? 'whitespace-pre-wrap break-all' : 'overflow-x-auto whitespace-pre',
        )}
      >
        <code>
          {showLineNumbers
            ? lines.map((line, idx) => (
                <div key={idx} className="flex">
                  <span className="w-8 shrink-0 select-none text-[var(--color-text-tertiary)] pe-3 text-end font-mono opacity-50">
                    {idx + 1}
                  </span>
                  <span className="flex-1">{line}</span>
                </div>
              ))
            : code}
        </code>
      </pre>
    </div>
  )
}
