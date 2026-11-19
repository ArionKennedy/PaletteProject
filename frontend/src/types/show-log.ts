export type ShowLog = {
  mediaId: string;
  title: string;
  mediaType: string;
  posterUrl: string | null;
  releaseDate: string | null;
  rating: number;
  body: string | null;
  lastReviewedAt: string;
};

export type ShowLogPage = { items: ShowLog[]; page: number; hasMore: boolean };
