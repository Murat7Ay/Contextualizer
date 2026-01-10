import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { cn } from '../../ui/utils';

function normalizeMarkdown(input: string): string {
  // Some handlers use HTML line breaks in output_format; keep rendering safe.
  return input.replace(/<br\s*\/?>/gi, '\n');
}

export function MarkdownViewer({ markdown }: { markdown: string }) {
  const md = normalizeMarkdown(markdown);

  return (
    <div className="text-sm leading-relaxed">
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        components={{
          h1: ({ className, ...props }) => (
            <h1 className={cn('text-2xl font-semibold mt-2 mb-3', className)} {...props} />
          ),
          h2: ({ className, ...props }) => (
            <h2 className={cn('text-xl font-semibold mt-6 mb-2', className)} {...props} />
          ),
          h3: ({ className, ...props }) => (
            <h3 className={cn('text-lg font-semibold mt-5 mb-2', className)} {...props} />
          ),
          h4: ({ className, ...props }) => (
            <h4 className={cn('text-base font-semibold mt-4 mb-2', className)} {...props} />
          ),
          p: ({ className, ...props }) => (
            <p className={cn('text-sm text-foreground/90 [&:not(:last-child)]:mb-3', className)} {...props} />
          ),
          a: ({ className, ...props }) => (
            <a className={cn('text-primary underline underline-offset-4', className)} {...props} />
          ),
          ul: ({ className, ...props }) => (
            <ul className={cn('list-disc pl-6 space-y-1 mb-3', className)} {...props} />
          ),
          ol: ({ className, ...props }) => (
            <ol className={cn('list-decimal pl-6 space-y-1 mb-3', className)} {...props} />
          ),
          li: ({ className, ...props }) => (
            <li className={cn('text-sm', className)} {...props} />
          ),
          blockquote: ({ className, ...props }) => (
            <blockquote
              className={cn('border-l-4 border-border pl-4 py-1 text-muted-foreground mb-3', className)}
              {...props}
            />
          ),
          hr: ({ className, ...props }) => <hr className={cn('my-4 border-border', className)} {...props} />,
          code: ({ className, children, ...props }) => (
            <code
              className={cn(
                'font-mono text-xs px-1.5 py-0.5 rounded bg-muted border border-border',
                className,
              )}
              {...props}
            >
              {children}
            </code>
          ),
          pre: ({ className, children, ...props }) => (
            <pre
              className={cn(
                'font-mono text-xs p-3 rounded-md bg-muted border border-border overflow-x-auto mb-3',
                className,
              )}
              {...props}
            >
              {children}
            </pre>
          ),
          table: ({ className, ...props }) => (
            <div className="overflow-x-auto mb-3">
              <table className={cn('w-full border border-border rounded-md', className)} {...props} />
            </div>
          ),
          th: ({ className, ...props }) => (
            <th className={cn('text-left text-xs font-semibold bg-muted px-3 py-2 border-b border-border', className)} {...props} />
          ),
          td: ({ className, ...props }) => (
            <td className={cn('text-sm px-3 py-2 border-b border-border align-top', className)} {...props} />
          ),
        }}
      >
        {md}
      </ReactMarkdown>
    </div>
  );
}


