import type { ShowToLog } from '@/types/show-log';

export const LOGGING_DEMO_ENABLED =
  __DEV__ && process.env.EXPO_PUBLIC_LOGGING_DEMO === 'true';
export const LOGGING_DEMO_TOKEN = 'palette-local-demo';

// Only a local test picker until Search & Discovery supplies a selected show.
// These are external TMDB TV IDs, not Palette database IDs.
export const DEMO_SHOWS: readonly ShowToLog[] = [
  { tmdbTvId: 1396, title: 'Breaking Bad' },
  { tmdbTvId: 66732, title: 'Stranger Things' },
  { tmdbTvId: 100088, title: 'The Last of Us' },
  { tmdbTvId: 2316, title: 'The Office' },
  { tmdbTvId: 246, title: 'Avatar: The Last Airbender' },
];
export const LOGGING_API_URL = process.env.EXPO_PUBLIC_API_URL?.replace(/\/$/, '');
export const LOGGING_REQUEST_TIMEOUT_MS = 15000;
