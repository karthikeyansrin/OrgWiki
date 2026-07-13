const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5051'

export type HealthStatus = {
  status: string
}

export async function getHealth(): Promise<HealthStatus> {
  const response = await fetch(`${apiBaseUrl}/health`)

  if (!response.ok) {
    throw new Error(`Backend health check failed with status ${response.status}.`)
  }

  return response.json() as Promise<HealthStatus>
}
