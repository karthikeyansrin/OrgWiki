import type { ReactNode } from 'react'

type Props = { content: string }

function inline(value: string, key: string): ReactNode[] {
  const parts = value.split(/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^\s)]+\)|\*[^*]+\*)/g)
  return parts.filter(Boolean).map((part, index) => {
    const nodeKey = `${key}-${index}`
    if (part.startsWith('**') && part.endsWith('**')) return <strong key={nodeKey}>{part.slice(2, -2)}</strong>
    if (part.startsWith('`') && part.endsWith('`')) return <code key={nodeKey} className="rounded bg-slate-100 px-1.5 py-0.5 text-[0.9em] text-slate-800">{part.slice(1, -1)}</code>
    const link = part.match(/^\[([^\]]+)\]\(([^\s)]+)\)$/)
    if (link) {
      const href = link[2]
      return /^(https?:\/\/|\/)/.test(href) ? <a key={nodeKey} href={href} className="font-medium text-teal-700 underline underline-offset-2 hover:text-teal-900" target={href.startsWith('http') ? '_blank' : undefined} rel={href.startsWith('http') ? 'noreferrer' : undefined}>{link[1]}</a> : link[1]
    }
    if (part.startsWith('*') && part.endsWith('*')) return <em key={nodeKey}>{part.slice(1, -1)}</em>
    return part
  })
}

function tableCells(line: string) {
  return line.trim().replace(/^\||\|$/g, '').split('|').map(cell => cell.trim())
}

function isTableDivider(line: string) {
  return /^\s*\|?\s*:?-{3,}:?\s*(\|\s*:?-{3,}:?\s*)+\|?\s*$/.test(line)
}

export function SafeMarkdown({ content }: Props) {
  const lines = content.replace(/\r\n/g, '\n').split('\n')
  const output: ReactNode[] = []
  let index = 0

  while (index < lines.length) {
    const line = lines[index]
    if (!line.trim()) { index++; continue }
    if (line.trimStart().startsWith('```')) {
      const code: string[] = []; index++
      while (index < lines.length && !lines[index].trimStart().startsWith('```')) code.push(lines[index++])
      if (index < lines.length) index++
      output.push(<pre key={`code-${index}`} className="overflow-x-auto rounded-lg bg-slate-950 p-5 text-sm leading-6 text-slate-100"><code>{code.join('\n')}</code></pre>)
      continue
    }
    if (line.includes('|') && index + 1 < lines.length && isTableDivider(lines[index + 1])) {
      const headers = tableCells(line); index += 2; const rows: string[][] = []
      while (index < lines.length && lines[index].includes('|') && lines[index].trim()) rows.push(tableCells(lines[index++]))
      output.push(<div key={`table-${index}`} className="overflow-x-auto rounded-lg border border-slate-200"><table className="min-w-full text-left text-sm"><thead className="bg-slate-50 text-slate-700"><tr>{headers.map((header, cell) => <th key={cell} className="px-4 py-3 font-semibold">{inline(header, `header-${cell}`)}</th>)}</tr></thead><tbody className="divide-y divide-slate-100">{rows.map((row, rowIndex) => <tr key={rowIndex}>{headers.map((_, cell) => <td key={cell} className="px-4 py-3 text-slate-700">{inline(row[cell] ?? '', `row-${rowIndex}-${cell}`)}</td>)}</tr>)}</tbody></table></div>)
      continue
    }
    const unordered = line.match(/^\s*[-*+]\s+(.+)$/)
    if (unordered) {
      const items: string[] = []
      while (index < lines.length) { const match = lines[index].match(/^\s*[-*+]\s+(.+)$/); if (!match) break; items.push(match[1]); index++ }
      output.push(<ul key={`unordered-${index}`} className="ml-6 list-disc space-y-1.5 text-slate-700">{items.map((item, itemIndex) => <li key={itemIndex}>{inline(item, `unordered-${itemIndex}`)}</li>)}</ul>)
      continue
    }
    const ordered = line.match(/^\s*\d+\.\s+(.+)$/)
    if (ordered) {
      const items: string[] = []
      while (index < lines.length) { const match = lines[index].match(/^\s*\d+\.\s+(.+)$/); if (!match) break; items.push(match[1]); index++ }
      output.push(<ol key={`ordered-${index}`} className="ml-6 list-decimal space-y-1.5 text-slate-700">{items.map((item, itemIndex) => <li key={itemIndex}>{inline(item, `ordered-${itemIndex}`)}</li>)}</ol>)
      continue
    }
    if (line.startsWith('### ')) output.push(<h3 key={index} className="mt-8 text-xl font-semibold text-slate-900">{inline(line.slice(4), `h3-${index}`)}</h3>)
    else if (line.startsWith('## ')) output.push(<h2 key={index} className="mt-10 text-2xl font-semibold text-slate-900">{inline(line.slice(3), `h2-${index}`)}</h2>)
    else if (line.startsWith('# ')) output.push(<h1 key={index} className="mt-10 text-3xl font-semibold text-slate-900">{inline(line.slice(2), `h1-${index}`)}</h1>)
    else if (line.startsWith('> ')) output.push(<blockquote key={index} className="border-l-4 border-teal-500 pl-4 leading-8 text-slate-600">{inline(line.slice(2), `quote-${index}`)}</blockquote>)
    else output.push(<p key={index} className="leading-8 text-slate-700">{inline(line, `paragraph-${index}`)}</p>)
    index++
  }

  return <div className="space-y-4">{output}</div>
}
