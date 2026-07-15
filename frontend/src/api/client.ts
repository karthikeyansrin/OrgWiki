const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5051'

export type HealthStatus = {
  status: string
}

export type IngestionDocument = {
  id: string
  fileName: string
  originalPath: string
  documentType: string
  processingStatus: string
  characterCount: number
  wordCount: number
  processingError: string | null
}

export type IngestionResult = {
  uploadId: string
  fileName: string
  status: string
  totalFiles: number
  supportedFiles: number
  parsedFiles: number
  failedFiles: number
  totalCharacterCount: number
  isEligibleForAnalysis: boolean
  analysisEligibilityReason: string | null
  documents: IngestionDocument[]
}
export type DiscoveryResult = { domains: { key: string; name: string; description: string; confidence: number }[]; topics: { key: string; name: string; description: string; domainKey: string; confidence: number; sourceDocumentIds: string[] }[]; relationships: { sourceTopicKey: string; targetTopicKey: string; type: string; explanation: string; confidence: number }[]; duplicateGroups: { title: string; explanation: string; confidence: number; topicKeys: string[]; sourceDocumentIds: string[] }[]; conflicts: { title: string; description: string; topicKeys: string[]; claimA: string; claimB: string; evidenceSnippetA: string; evidenceSnippetB: string; recommendation: string; recommendationReasoning: string; confidence: number }[]; outdatedCandidates: { description: string; reason: string; topicKeys: string[]; confidence: number }[]; suggestedArticles: { key: string; title: string; summary: string; domainKey: string; topicKeys: string[]; sourceDocumentIds: string[]; reason: string; confidence: number }[] }
export type AnalysisResult = { analysisId: string; uploadId: string; status: string; aiMode: string; model: string; documentsAnalyzed: number; corpusCharacterCount: number; inputTokens: number | null; outputTokens: number | null; totalTokens: number | null; durationMilliseconds: number | null; errorMessage: string | null; discovery: DiscoveryResult | null }
export type GeneratedArticle = { key: string; title: string; summary: string; markdownContent: string; difficulty: string; estimatedReadingMinutes: number; tags: string[]; relatedArticleKeys: string[]; confidence: number; citations: { sourceDocumentId: string; evidenceSnippet: string }[] }
export type GenerationResult = { generationId: string; analysisId: string; status: string; aiMode: string; model: string; inputTokens: number; outputTokens: number; totalTokens: number; durationMilliseconds: number | null; errorMessage: string | null; result: { articles: GeneratedArticle[] } | null }
export type ReviewListArticle = { id: string; key: string; title: string; summary: string; status: string; confidence: number; citationCount: number; lastEditedAtUtc: string | null; tags: string[]; difficulty: string; estimatedReadingMinutes: number; domain: string }
export type ReviewDashboard = { pendingReview: number; approved: number; rejected: number; published: number; articles: ReviewListArticle[] }
export type ReviewArticle = ReviewListArticle & { key: string; markdownContent: string; relatedArticleKeys: string[]; reviewNotes: string | null; reviewedAtUtc: string | null; reviewedBy: string | null; lastEditedBy: string | null; citations: { sourceDocumentId: string; fileName: string; originalPath: string; evidenceSnippet: string }[]; availableRelatedArticles: ReviewListArticle[]; quality: { citationCount: number; sourceDocumentCount: number; confidence: number; relatedArticleCount: number; linkedConflictCount: number; potentiallyOutdatedCount: number } }
export type KnowledgeArticleSummary = { key: string; title: string; summary: string; domain: string; tags: string[]; difficulty: string; estimatedReadingMinutes: number; publishedAtUtc: string; relatedArticleCount: number }
export type KnowledgeBaseHome = { publishedArticleCount: number; domains: string[]; tags: string[]; articles: KnowledgeArticleSummary[] }
export type KnowledgeArticle = KnowledgeArticleSummary & { markdownContent: string; relatedArticles: { key: string; title: string; summary: string }[]; citations: { sourceFileName: string; sourcePath: string; evidenceSnippet: string }[] }

export async function getHealth(): Promise<HealthStatus> {
  const response = await fetch(`${apiBaseUrl}/health`)

  if (!response.ok) {
    throw new Error(`Backend health check failed with status ${response.status}.`)
  }

  return response.json() as Promise<HealthStatus>
}

async function readError(response: Response) {
  try {
    const body = (await response.json()) as { error?: string; detail?: string }
    return body.error ?? body.detail ?? 'The request could not be completed.'
  } catch {
    return 'The request could not be completed.'
  }
}

export async function uploadArchive(file: File): Promise<IngestionResult> {
  const form = new FormData()
  form.append('file', file)
  const response = await fetch(`${apiBaseUrl}/api/uploads`, { method: 'POST', body: form })
  if (!response.ok) throw new Error(await readError(response))
  return response.json() as Promise<IngestionResult>
}

export async function getUpload(uploadId: string): Promise<IngestionResult> {
  const response = await fetch(`${apiBaseUrl}/api/uploads/${uploadId}`)
  if (!response.ok) throw new Error(await readError(response))
  return response.json() as Promise<IngestionResult>
}

export async function startAnalysis(uploadId: string, retry = false): Promise<AnalysisResult> {
  const response = await fetch(`${apiBaseUrl}/api/uploads/${uploadId}/analysis?retry=${retry}`, { method: 'POST' })
  if (!response.ok) throw new Error(await readError(response))
  return response.json() as Promise<AnalysisResult>
}

export async function getAnalysis(analysisId: string): Promise<AnalysisResult> {
  const response = await fetch(`${apiBaseUrl}/api/analyses/${analysisId}`)
  if (!response.ok) throw new Error(await readError(response))
  return response.json() as Promise<AnalysisResult>
}

export async function startGeneration(analysisId: string, retry = false): Promise<GenerationResult> {
  const response = await fetch(`${apiBaseUrl}/api/analyses/${analysisId}/generate?retry=${retry}`, { method: 'POST' })
  if (!response.ok) throw new Error(await readError(response))
  return response.json() as Promise<GenerationResult>
}

export async function getGeneration(generationId: string): Promise<GenerationResult> {
  const response = await fetch(`${apiBaseUrl}/api/generations/${generationId}`)
  if (!response.ok) throw new Error(await readError(response))
  return response.json() as Promise<GenerationResult>
}

export async function getReviewDashboard(): Promise<ReviewDashboard> {
  const response = await fetch(`${apiBaseUrl}/api/review`)
  if (!response.ok) throw new Error(await readError(response))
  return response.json() as Promise<ReviewDashboard>
}

export async function getReviewArticle(id: string): Promise<ReviewArticle> {
  const response = await fetch(`${apiBaseUrl}/api/review/articles/${id}`)
  if (!response.ok) throw new Error(await readError(response))
  return response.json() as Promise<ReviewArticle>
}

export async function updateReviewArticle(id: string, article: Pick<ReviewArticle, 'title' | 'summary' | 'markdownContent' | 'difficulty' | 'estimatedReadingMinutes' | 'tags' | 'relatedArticleKeys'>): Promise<ReviewArticle> {
  const response = await fetch(`${apiBaseUrl}/api/review/articles/${id}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(article) })
  if (!response.ok) throw new Error(await readError(response))
  return response.json() as Promise<ReviewArticle>
}

async function reviewAction(id: string, action: 'approve' | 'reject', notes: string): Promise<ReviewArticle> {
  const response = await fetch(`${apiBaseUrl}/api/review/articles/${id}/${action}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ notes }) })
  if (!response.ok) throw new Error(await readError(response))
  return response.json() as Promise<ReviewArticle>
}
export const approveReviewArticle = (id: string, notes: string) => reviewAction(id, 'approve', notes)
export const rejectReviewArticle = (id: string, notes: string) => reviewAction(id, 'reject', notes)
export async function publishReviewArticle(id: string): Promise<ReviewArticle> {
  const response = await fetch(`${apiBaseUrl}/api/review/articles/${id}/publish`, { method: 'POST' })
  if (!response.ok) throw new Error(await readError(response))
  return response.json() as Promise<ReviewArticle>
}
export async function getKnowledgeBase(): Promise<KnowledgeBaseHome> { const response = await fetch(`${apiBaseUrl}/api/knowledge`); if (!response.ok) throw new Error(await readError(response)); return response.json() as Promise<KnowledgeBaseHome> }
export async function searchKnowledge(query: string): Promise<KnowledgeArticleSummary[]> { const response = await fetch(`${apiBaseUrl}/api/knowledge/search?q=${encodeURIComponent(query)}`); if (!response.ok) throw new Error(await readError(response)); return response.json() as Promise<KnowledgeArticleSummary[]> }
export async function getKnowledgeArticle(key: string): Promise<KnowledgeArticle> { const response = await fetch(`${apiBaseUrl}/api/knowledge/articles/${encodeURIComponent(key)}`); if (!response.ok) throw new Error(await readError(response)); return response.json() as Promise<KnowledgeArticle> }
