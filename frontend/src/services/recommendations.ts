import { fetchWithAuth } from '@/services/api';
import type { RecommendedMovie } from '@/types/recommendedMovie';
import type { MediaSearchResult } from '@/types/mediaSearchResult';

export async function getMovieRecommendations(
  limit = 20,
): Promise<MediaSearchResult[]> {
  const response = await fetchWithAuth(
    `/api/recommendations/movies?limit=${limit}`,
  );

  if (!response.ok) {
    throw new Error(
      `Failed to load recommendations (${response.status})`,
    );
  }

  const movies: RecommendedMovie[] = await response.json();

  return movies.map((movie) => ({
    externalId: String(movie.tmdbMovieId),
    mediaType: 'movie',
    title: movie.title,
    releaseDate: movie.releaseDate,
    posterUrl: movie.posterPath
      ? `https://image.tmdb.org/t/p/w500${movie.posterPath}`
      : null,
  }));
}