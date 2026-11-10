export type BookLookupResult = {
  externalId: string;
  mediaType: string;
  title: string;
  releaseDate: string | null;   
  posterUrl: string | null;
  description: string | null;
  isbn: string | null;
};