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
