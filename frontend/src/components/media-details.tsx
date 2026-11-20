import { useEffect, useState } from 'react';
import type { Href } from 'expo-router';

import MediaDetailsPage from '@/components/media-details-page';
import { bookLookup, movieLookup, tvLookup } from '@/services/lookup';
import type { BookLookupResult } from '@/types/bookLookupResult';
import type { LibraryMediaType } from '@/types/library';
import type { MovieLookupResult } from '@/types/movieLookupResult';
import type { TvLookupResult } from '@/types/tvLookupResult';

type LookupResult = MovieLookupResult | TvLookupResult | BookLookupResult;

const lookups = { movie: movieLookup, tv: tvLookup, book: bookLookup };
const titles = { movie: 'Movie', tv: 'TV Show', book: 'Book' };

export function MediaDetails({ id, type, backHref }: { id: string; type: LibraryMediaType; backHref?: Href }) {
  const [media, setMedia] = useState<LookupResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    lookups[type](id, controller.signal)
      .then(result => { if (!controller.signal.aborted) setMedia(result); })
      .catch((problem: Error) => {
        if (!controller.signal.aborted) setError(problem.message);
      });
    return () => controller.abort();
  }, [type, id]);

  return <MediaDetailsPage media={media} error={error} metaText={media ? getMetaText(media) : ''}
    errorTitle={titles[type]} type={type} backHref={backHref} />;
}

function getMetaText(media: LookupResult): string {
  const year = media.releaseDate?.slice(0, 4);
  if ('runtime' in media) {
    return [year ?? 'Release date unknown', media.runtime != null && formatRuntime(media.runtime)]
      .filter(Boolean).join(' · ');
  }
  if ('numberOfSeasons' in media) {
    const seasons = media.numberOfSeasons;
    return [year && `First Air: ${year}`, seasons && `${seasons} ${seasons === 1 ? 'Season' : 'Seasons'}`]
      .filter(Boolean).join(' · ');
  }
  return [year && `Published: ${year}`, media.isbn && `ISBN: ${media.isbn}`].filter(Boolean).join(' · ');
}

function formatRuntime(minutes: number): string {
  const hours = Math.floor(minutes / 60);
  const rest = minutes % 60;
  return hours > 0 ? `${hours}h ${rest}m` : `${rest}m`;
}
