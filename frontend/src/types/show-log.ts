export type ShowLog = {
  logId: string;
  mediaId: string;
  tmdbTvId: number;
  title: string;
  status: string | null;
  loggedDate: string;
};

export type ShowLogPage = { items: ShowLog[]; page: number; hasMore: boolean };
export type ShowToLog = { tmdbTvId: number; title: string };
