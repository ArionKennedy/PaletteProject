import {
  LOGGING_API_URL, LOGGING_DEMO_ENABLED, LOGGING_REQUEST_TIMEOUT_MS,
} from '@/constants/logging';
import type { ShowLog, ShowLogPage, ShowToLog } from '@/types/show-log';

async function request<T>(accessToken: string | null | undefined, path: string,
  options: RequestInit = {}): Promise<T> {
  if (!accessToken) throw new Error('Sign in before opening your shows.');
  if (!LOGGING_API_URL) throw new Error('Set EXPO_PUBLIC_API_URL to connect to the backend.');

  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), LOGGING_REQUEST_TIMEOUT_MS);
  try {
    const response = await fetch(`${LOGGING_API_URL}/api/me/shows${path}`, {
      ...options,
      signal: controller.signal,
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${accessToken}`,
      },
    });
    if (!response.ok) {
      if (response.status === 401 || response.status === 403)
        throw new Error(LOGGING_DEMO_ENABLED
          ? 'Local demo access was not accepted. Enable LoggingDemo__Enabled on the development backend and use localhost.'
          : 'The logging API did not accept your sign-in. Backend Clerk token validation and profile integration are still required.');
      if (response.status === 404)
        throw new Error('Your profile or the logging endpoint is not available yet.');
      if (response.status === 400)
        throw new Error('The show could not be logged. Check the selected show and try again.');
      throw new Error('The server could not complete the request. Please try again.');
    }
    return await response.json() as T;
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

export function getShowLogs(accessToken: string | null | undefined, page = 1) {
  return request<ShowLogPage>(accessToken, `?page=${page}`);
}

export function saveWatchedShow(accessToken: string | null | undefined, show: ShowToLog) {
  return request<ShowLog>(accessToken, '', { method: 'POST', body: JSON.stringify(show) });
}
