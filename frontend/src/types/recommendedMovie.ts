export type RecommendedMovie = {
  tmdbMovieId: number;
  title: string;
  posterPath: string | null;
  releaseDate: string | null;
  finalScore: number;
  contentScore: number | null;
  popularityScore: number;
  ratingQualityScore: number;
};