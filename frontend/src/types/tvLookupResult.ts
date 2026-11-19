export type TvLookupResult = {
  externalId: string;
  saved?: boolean;
  mediaType: string;
  title: string;
  releaseDate: string | null;   
  posterUrl: string | null;
  description: string | null;
  numberOfSeasons: number | null;
};