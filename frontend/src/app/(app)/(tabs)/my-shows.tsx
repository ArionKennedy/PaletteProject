import { useCallback, useRef, useState } from 'react';
import { useFocusEffect } from 'expo-router';

import { useAuth } from '@clerk/expo';
import { ShowList } from '@/components/show-list';
import { getShowLogs } from '@/services/show-logs';
import type { ShowLog } from '@/types/show-log';

export default function MyShowsScreen() {
  const { isLoaded, isSignedIn, userId, sessionId } = useAuth();
  const [shows, setShows] = useState<ShowLog[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(false);
  const version = useRef(0);
  const focused = useRef(false);
  const load = useCallback(async (nextPage = 1) => {
    if (!isLoaded || !isSignedIn || !userId || !focused.current) return;
    const currentVersion = ++version.current;
    setLoading(true);
    setError(null);
    try {
      const result = await getShowLogs(userId, nextPage);
      if (currentVersion !== version.current) return;
      setShows(previous => nextPage === 1 ? result.items : [
        ...previous,
        ...result.items.filter(item => !previous.some(existing => existing.mediaId === item.mediaId)),
      ]);
      setPage(result.page);
      setHasMore(result.hasMore);
    } catch (problem) {
      if (currentVersion === version.current)
        setError(problem instanceof Error ? problem.message : 'Could not load your shows.');
    } finally {
      if (currentVersion === version.current) setLoading(false);
    }
  }, [isLoaded, isSignedIn, userId]);

  useFocusEffect(useCallback(() => {
    // A different Clerk user/session starts a new screen lifecycle.
    focused.current = Boolean(userId && sessionId);
    setLoading(false);
    setShows([]);
    setHasMore(false);
    setError(null);
    void load();
    return () => {
      focused.current = false;
      version.current += 1;
    };
  }, [load, userId, sessionId]));

  return (
    <ShowList shows={shows} loading={!isLoaded || !isSignedIn || loading} error={error}
      hasMore={hasMore}
      onRefresh={() => void load()}
      onLoadMore={() => { if (!loading && hasMore) void load(page + 1); }} />
  );
}
