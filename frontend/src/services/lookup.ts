import type { MovieLookupResult } from '@/types/movieLookupResult';
import { getJson } from '@/services/client';

export function movieLookup(movieId: string, signal?: AbortSignal): Promise<MovieLookupResult> {
  return getJson<MovieLookupResult>(`/api/lookup/movie/${encodeURIComponent(movieId)}`, signal);
}
