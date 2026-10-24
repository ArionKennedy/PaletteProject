import { useCallback, useRef, useState } from 'react';
import { useFocusEffect } from 'expo-router';
import { Pressable, StyleSheet, View } from 'react-native';

import { useAuth } from '@clerk/expo';
import { ShowList } from '@/components/show-list';
import { Text } from '@/components/ui/text';
import { DEMO_SHOWS, LOGGING_DEMO_ENABLED, LOGGING_DEMO_TOKEN } from '@/constants/logging';
import { Spacing } from '@/lib/theme';
import { useTheme } from '@/hooks/use-theme';
import { getShowLogs, saveWatchedShow } from '@/services/show-logs';
import type { ShowLog, ShowToLog } from '@/types/show-log';

export default function MyShowsScreen() {
  const { getToken, isLoaded, isSignedIn, userId, sessionId } = useAuth();
  const theme = useTheme();
  const [shows, setShows] = useState<ShowLog[]>([]);
  const [loading, setLoading] = useState(false);
  const [savingId, setSavingId] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(false);
  const version = useRef(0);
  const focused = useRef(false);
  const saving = useRef(false);
  const lifecycle = useRef(0);

  const getAccessToken = useCallback(async () => {
    if (!isLoaded || !isSignedIn) throw new Error('Sign in before opening your shows.');
    // Demo mode is an explicit choice, never a fallback for rejected Clerk tokens.
    return LOGGING_DEMO_ENABLED ? LOGGING_DEMO_TOKEN : await getToken();
  }, [getToken, isLoaded, isSignedIn]);

  const load = useCallback(async (nextPage = 1) => {
    if (!isLoaded || !isSignedIn || !focused.current) return;
    const currentVersion = ++version.current;
    setLoading(true);
    setError(null);
    try {
      const accessToken = await getAccessToken();
      if (currentVersion !== version.current) return;
      const result = await getShowLogs(accessToken, nextPage);
      if (currentVersion !== version.current) return;
      setShows(previous => nextPage === 1 ? result.items : [
        ...previous,
        ...result.items.filter(item => !previous.some(existing => existing.logId === item.logId)),
      ]);
      setPage(result.page);
      setHasMore(result.hasMore);
    } catch (problem) {
      if (currentVersion === version.current)
        setError(problem instanceof Error ? problem.message : 'Could not load your shows.');
    } finally {
      if (currentVersion === version.current) setLoading(false);
    }
  }, [getAccessToken, isLoaded, isSignedIn]);

  useFocusEffect(useCallback(() => {
    lifecycle.current += 1;
    // A different Clerk user/session starts a new screen lifecycle.
    focused.current = Boolean(userId && sessionId);
    saving.current = false;
    setSavingId(null);
    setLoading(false);
    setShows([]);
    setHasMore(false);
    setError(null);
    setMessage(null);
    void load();
    return () => {
      focused.current = false;
      lifecycle.current += 1;
      version.current += 1;
    };
  }, [load, userId, sessionId]));

  async function markWatched(show: ShowToLog) {
    if (saving.current || !focused.current || !isLoaded || !isSignedIn) return;
    const currentLifecycle = lifecycle.current;
    const isCurrent = () => focused.current && currentLifecycle === lifecycle.current;
    saving.current = true;
    setSavingId(show.tmdbTvId);
    setError(null);
    setMessage(null);
    // Ignore an older history request that might finish after this save.
    version.current += 1;
    try {
      const accessToken = await getAccessToken();
      if (!isCurrent()) return;
      await saveWatchedShow(accessToken, show);
      if (!isCurrent()) return;
      setMessage(`${show.title} was saved to your history.`);
      await load();
    } catch (problem) {
      if (isCurrent())
        setError(problem instanceof Error ? problem.message : 'Could not save this show. Try again.');
    } finally {
      if (isCurrent()) {
        saving.current = false;
        setSavingId(null);
        setLoading(false);
      }
    }
  }

  return (
    <ShowList shows={shows} loading={!isLoaded || !isSignedIn || loading || savingId !== null} error={error}
      message={message} hasMore={hasMore}
      onRefresh={() => { setMessage(null); void load(); }}
      onLoadMore={() => void load(page + 1)}>
      {LOGGING_DEMO_ENABLED && (
        <View style={styles.picker}>
          <Text style={{ color: theme.textSecondary }}>
            Local demo · These shows belong to a shared local demo profile, not your Clerk account.
            Choose a sample show to test saving to the database.
          </Text>
          {DEMO_SHOWS.map(show => {
            const watched = shows.some(item => item.tmdbTvId === show.tmdbTvId && item.status === 'watched');
            return (
              <Pressable key={show.tmdbTvId} accessibilityRole="button"
                accessibilityLabel={`Mark ${show.title} as watched`}
                disabled={!isLoaded || !isSignedIn || loading || savingId !== null || watched}
                onPress={() => void markWatched(show)}
                style={[styles.choice, { backgroundColor: theme.backgroundElement },
                  (savingId !== null || watched) && styles.disabled]}>
                <Text style={{ color: theme.text }}>{show.title}</Text>
                <Text variant="small" style={{ color: theme.textSecondary }}>
                  {savingId === show.tmdbTvId ? 'Saving…' : watched ? 'Watched' : 'Mark as watched'}
                </Text>
              </Pressable>
            );
          })}
        </View>
      )}
    </ShowList>
  );
}

const styles = StyleSheet.create({
  picker: { gap: Spacing.two },
  choice: { padding: Spacing.three, borderRadius: Spacing.two },
  disabled: { opacity: 0.6 },
});
