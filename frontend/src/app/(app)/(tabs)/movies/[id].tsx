import { useEffect, useState } from 'react';
import { useLocalSearchParams } from 'expo-router';

import { movieLookup } from '@/services/lookup';
import type { MovieLookupResult } from '@/types/movieLookupResult';
import MediaDetailsPage from '@/components/media-details-page';

// 148 -> "2h 28m", 45 -> "45m"
function formatRuntime(minutes: number): string {
  const hours = Math.floor(minutes / 60);
  const rest = minutes % 60;
  return hours > 0 ? `${hours}h ${rest}m` : `${rest}m`;
}

export default function MovieDetailsPage() {
  const { id } = useLocalSearchParams<{ id: string }>();   // "27205" from /movies/27205
  return <MovieDetails key={id} id={id} />;
}

function MovieDetails({ id }: { id: string }) {
  const [movie, setMovie] = useState<MovieLookupResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    movieLookup(id, controller.signal)
      .then(setMovie)
      .catch((e: Error) => {
        if (e.name !== 'AbortError') setError(e.message);
      });

    // Cancel the request if the user leaves before it finishes.
    return () => controller.abort();
  }, [id]);

  const metaText = movie
      ? [movie.releaseDate?.slice(0, 4) ?? 'Release date unknown', movie.runtime != null && formatRuntime(movie.runtime)]
          .filter(Boolean)
          .join(' · ')
      : '';

  return (<MediaDetailsPage media={movie} error={error} metaText={metaText} errorTitle="Movie" />);
}