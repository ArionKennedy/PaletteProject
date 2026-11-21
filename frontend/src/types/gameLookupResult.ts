export type GameLookupResult = {
  externalId: string;
  saved?: boolean;
  title: string;
  releaseDate: string | null;
  description: string | null;
  posterUrl: string | null;
};
