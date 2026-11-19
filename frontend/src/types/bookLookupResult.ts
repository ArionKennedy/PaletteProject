export type BookLookupResult = {
  externalId: string;
  saved?: boolean;
  title: string;
  releaseDate: string | null;
  description: string | null;
  posterUrl: string | null;
  isbn: string | null;
};
