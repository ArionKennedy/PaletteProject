export type TvLookupResult = {
  externalId: string;
  mediaType: string;
  title: string;
  releaseDate: string | null;   
  posterUrl: string | null;
  description: string | null;
  numberOfSeasons: number | null;
};