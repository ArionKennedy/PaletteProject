import type { MovieLookupResult } from '@/types/movieLookupResult';
import { getJson } from '@/services/client';
import { TvLookupResult } from '@/types/tvLookupResult';
import { BookLookupResult } from '@/types/bookLookupResult';

export function movieLookup(movieId: string, signal?: AbortSignal): Promise<MovieLookupResult> {
  return getJson<MovieLookupResult>(`/api/lookup/movie/${encodeURIComponent(movieId)}`, signal);
}

export function tvLookup(tvId: string, signal?: AbortSignal): Promise<TvLookupResult> {
  return getJson<TvLookupResult>(`/api/lookup/tv/${encodeURIComponent(tvId)}`, signal);
}

export function bookLookup(bookId: string, signal?: AbortSignal): Promise<BookLookupResult> {
  return getJson<BookLookupResult>(`/api/lookup/book/${encodeURIComponent(bookId)}`, signal);
}