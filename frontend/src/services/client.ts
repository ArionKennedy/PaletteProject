const API_URL = process.env.EXPO_PUBLIC_API_URL;

// Shared GET helper: builds the URL, checks the response, and turns
// backend ProblemDetails errors into an Error with a readable message.
export async function getJson<T>(path: string, signal?: AbortSignal): Promise<T> {
  if (!API_URL) {
    throw new Error('API URL is not configured');
  }

  const response = await fetch(`${API_URL}${path}`, { signal });

  if (!response.ok) {
    const problem: { detail?: string } | null = await response.json().catch(() => null);
    throw new Error(problem?.detail ?? `API request failed: ${response.status}`);
  }

  return response.json();
}
