import type { MediaSearchResult } from '@/types/mediaSearchResult';
import { getJson } from '@/services/client';

export async function mediaSearch(
  mediaType: string,
  query: string,
  signal?: AbortSignal,
): Promise<MediaSearchResult[]> {
  if (
    typeof mediaType !== 'string' ||
    typeof query !== 'string' ||
    !mediaType.trim() ||
    !query.trim()
  ) {
    return [];
  }

  const path = `/api/search/${encodeURIComponent(mediaType)}?q=${encodeURIComponent(query)}`;
  const data = await getJson<{ results: MediaSearchResult[] }>(path, signal);

  return data?.results ?? [];
}
