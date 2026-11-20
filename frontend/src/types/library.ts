import type { MediaSearchResult } from '@/types/mediaSearchResult';

export const LIBRARY_CATEGORIES = [
  { type: 'movie', label: 'Movies' },
  { type: 'tv', label: 'TV Shows' },
  { type: 'book', label: 'Books' },
] as const;

export type LibraryMediaType = (typeof LIBRARY_CATEGORIES)[number]['type'];
export type LibraryPreviews = Record<LibraryMediaType, MediaSearchResult[]>;
export type LibraryPage = { items: MediaSearchResult[]; page: number; hasMore: boolean };
export type LibraryMembership = { saved: boolean };
