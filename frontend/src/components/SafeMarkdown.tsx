import type { ReactNode } from 'react'

type Props = { content: string }

export function SafeMarkdown({ content }: Props) {
  const lines = content.split('\n'); const output: ReactNode[] = []; let code: string[] = []; let inCode = false
  const flushCode = () => { if (code.length) output.push(<pre key={`code-${output.length}`} className="overflow-x-auto rounded-lg bg-slate-950 p-5 text-sm leading-6 text-slate-100"><code>{code.join('\n')}</code></pre>); code = [] }
  lines.forEach((line, index) => {
    if (line.trimStart().startsWith('```')) { if (inCode) flushCode(); inCode = !inCode; return }
    if (inCode) { code.push(line); return }
    if (!line.trim()) return
    if (line.startsWith('### ')) output.push(<h3 key={index} className="mt-6 text-xl font-semibold">{line.slice(4)}</h3>)
    else if (line.startsWith('## ')) output.push(<h2 key={index} className="mt-8 text-2xl font-semibold">{line.slice(3)}</h2>)
    else if (line.startsWith('# ')) output.push(<h1 key={index} className="mt-8 text-3xl font-semibold">{line.slice(2)}</h1>)
    else if (line.startsWith('> ')) output.push(<blockquote key={index} className="border-l-4 border-teal-500 pl-4 italic text-slate-600">{line.slice(2)}</blockquote>)
    else if (/^[-*] /.test(line)) output.push(<li key={index} className="ml-6 list-disc">{line.slice(2)}</li>)
    else if (line.includes('|')) output.push(<pre key={index} className="overflow-x-auto rounded bg-slate-100 p-3 text-sm">{line}</pre>)
    else output.push(<p key={index} className="leading-8 text-slate-700">{line}</p>)
  }); if (inCode) flushCode(); return <div className="space-y-4">{output}</div>
}
