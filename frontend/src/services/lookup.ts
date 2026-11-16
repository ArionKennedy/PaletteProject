import type { MovieLookupResult } from '@/types/movieLookupResult';
import { getJson } from '@/services/client';
import { TvLookupResult } from '@/types/tvLookupResult';

export function movieLookup(movieId: string, signal?: AbortSignal): Promise<MovieLookupResult> {
  return getJson<MovieLookupResult>(`/api/lookup/movie/${encodeURIComponent(movieId)}`, signal);
}

export function tvLookup(tvId: string, signal?: AbortSignal): Promise<TvLookupResult> {
  return getJson<TvLookupResult>(`/api/lookup/tv/${encodeURIComponent(tvId)}`, signal);
}