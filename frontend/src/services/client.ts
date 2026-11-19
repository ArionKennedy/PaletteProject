import { fetchWithAuth } from '@/services/api';

// Shared GET helper: checks the response, and turns
// backend ProblemDetails errors into an Error with a readable message.
export async function getJson<T>(path: string, signal?: AbortSignal): Promise<T> {
  const response = await fetchWithAuth(`${path}`, { signal });

  if (!response.ok) {
    const problem: { detail?: string } | null = await response.json().catch(() => null);
    throw new Error(problem?.detail ?? `API request failed: ${response.status}`);
  }

  return response.json();
}
