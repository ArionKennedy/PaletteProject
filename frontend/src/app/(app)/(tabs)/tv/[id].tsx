import { useEffect, useState } from 'react';
import { useLocalSearchParams } from 'expo-router';

import type { TvLookupResult } from '@/types/tvLookupResult';
import { tvLookup } from '@/services/lookup';
import MediaDetailsPage from '@/components/media-details-page';


export default function TvDetailsPage() {
  const { id } = useLocalSearchParams<{ id: string }>();   // "27205" from /movies/27205
  const [tv, setTv] = useState<TvLookupResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    setTv(null);
    setError(null);

    tvLookup(id, controller.signal)
      .then(setTv)
      .catch((e: Error) => {
        if (e.name !== 'AbortError') setError(e.message);
      });

    // Cancel the request if the user leaves before it finishes.
    return () => controller.abort();
  }, [id]);

  const releaseYear = tv?.releaseDate?.slice(0, 4);
  const seasons = tv?.numberOfSeasons
    ? `${tv.numberOfSeasons} ${tv.numberOfSeasons === 1 ? 'Season' : 'Seasons'}`
    : null;

  const metaText = tv ?
  [
    releaseYear ? `First Air: ${releaseYear}` : null,
    seasons,
  ]
    .filter(Boolean)
    .join(' · ') : '';

  return (<MediaDetailsPage media={tv} error={error} metaText={metaText} errorTitle="TV Show" />);
}