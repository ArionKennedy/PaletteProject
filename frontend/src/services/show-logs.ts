import { LOGGING_REQUEST_TIMEOUT_MS } from '@/constants/logging';
import { fetchWithAuth } from '@/services/api';
import type { ShowLogPage } from '@/types/show-log';

export async function getShowLogs(page = 1): Promise<ShowLogPage> {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), LOGGING_REQUEST_TIMEOUT_MS);
  try {
    const response = await fetchWithAuth('/api/me/shows/query', {
      method: 'POST',
      signal: controller.signal,
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ page }),
    });
    if (!response.ok) {
      if (response.status === 401 || response.status === 403)
        throw new Error('Your session was not accepted. Sign in again and retry.');
      if (response.status === 404)
        throw new Error('Your profile or the logging endpoint is not available yet.');
      if (response.status === 400)
        throw new Error('The history request is invalid. Refresh and try again.');
      throw new Error('The server could not complete the request. Please try again.');
    }
    return await response.json() as ShowLogPage;
  } catch (error) {
    if (controller.signal.aborted)
      throw new Error('The request timed out. Check the backend connection and try again.');
    if (error instanceof TypeError)
      throw new Error('Cannot reach the backend. Check that it is running and try again.');
    throw error;
  } finally {
    clearTimeout(timeout);
  }
}
