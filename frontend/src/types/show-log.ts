export type ShowLog = {
  mediaId: string;
  title: string;
  mediaType: string;
  lastReviewedAt: string;
};

export type ShowLogPage = { items: ShowLog[]; page: number; hasMore: boolean };
