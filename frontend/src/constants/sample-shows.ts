export type ShowListItem = {
  mediaId: string;
  title: string;
};

// Frontend demo data. 
// This is to be replaced with the current user's shows when the API is ready.
export const SAMPLE_SHOWS: readonly ShowListItem[] = [
  { mediaId: 'sample-1', title: 'Breaking Bad' },
  { mediaId: 'sample-2', title: 'Stranger Things' },
  { mediaId: 'sample-3', title: 'The Last of Us' },
  { mediaId: 'sample-4', title: 'The Office' },
  { mediaId: 'sample-5', title: 'Avatar: The Last Airbender' },
];
