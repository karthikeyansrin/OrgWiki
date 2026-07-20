type ArticleForDownload = {
  title: string
  summary: string
  markdownContent: string
  tags: string[]
  generatedAtUtc: string
  publishedAtUtc: string
}

function markdownFilename(title: string) {
  const name = title.toLowerCase().trim().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '')
  return `${name || 'orgwiki-article'}.md`
}

export function downloadArticleMarkdown(article: ArticleForDownload) {
  const markdown = `# ${article.title}\n\n## Summary\n\n${article.summary}\n\n## Tags\n\n${article.tags.length ? article.tags.map(tag => `- ${tag}`).join('\n') : '- None'}\n\n## Generated\n\n${new Date(article.generatedAtUtc).toLocaleString()}\n\n## Published\n\n${new Date(article.publishedAtUtc).toLocaleString()}\n\n---\n\n${article.markdownContent}\n`
  const url = URL.createObjectURL(new Blob([markdown], { type: 'text/markdown;charset=utf-8' }))
  const link = document.createElement('a')
  link.href = url
  link.download = markdownFilename(article.title)
  document.body.append(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}
