import { LOGGING_REQUEST_TIMEOUT_MS } from '@/constants/logging';
import { fetchWithAuth } from '@/services/api';
import type { LibraryMediaType, LibraryMembership, LibraryPage, LibraryPreviews } from '@/types/library';

const LIBRARY_PATH = '/api/me/library';

async function requestLibrary<T>(path: string, method: 'GET' | 'PUT' | 'DELETE' = 'GET'): Promise<T> {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), LOGGING_REQUEST_TIMEOUT_MS);
  try {
    const response = await fetchWithAuth(path, { method, signal: controller.signal });
    if (response.status === 401 || response.status === 403) {
      throw new Error('Your session was not accepted. Sign in again and retry.');
    }
    if (!response.ok) {
      const problem: { detail?: string; message?: string } | null = await response.json().catch(() => null);
      throw new Error(problem?.detail ?? problem?.message ?? 'Could not update your library. Please try again.');
    }
    return await response.json();
  } catch (error) {
    if (controller.signal.aborted) {
      throw new Error('The request timed out. Try again.');
    }
    if (error instanceof TypeError) {
      throw new Error('Cannot reach the backend. Check your connection and try again.');
    }
    throw error;
  } finally {
    clearTimeout(timeout);
  }
}

export function getLibraryPreviews() {
  return requestLibrary<LibraryPreviews>(`${LIBRARY_PATH}/previews`);
}

export function getLibraryPage(type: LibraryMediaType, page = 1) {
  return requestLibrary<LibraryPage>(`${LIBRARY_PATH}/${type}?page=${page}`);
}

function itemPath(type: LibraryMediaType, id: string) {
  return `${LIBRARY_PATH}/${type}/${encodeURIComponent(id)}`;
}

export function getLibraryMembership(type: LibraryMediaType, id: string) {
  return requestLibrary<LibraryMembership>(itemPath(type, id));
}

export function setLibraryMembership(type: LibraryMediaType, id: string, saved: boolean) {
  return requestLibrary<LibraryMembership>(itemPath(type, id), saved ? 'PUT' : 'DELETE');
}
