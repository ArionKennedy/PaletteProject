import { useCallback, useRef, useState } from 'react';
import { Link, Stack, useFocusEffect } from 'expo-router';
import { ActivityIndicator, FlatList, Platform, Pressable, StyleSheet, Text, View } from 'react-native';

import Movie from '@/components/movie';
import { Button } from '@/components/ui/button';
import { WEB_TAB_BAR_OFFSET } from '@/constants/moviesConstants';
import { useTheme } from '@/hooks/use-theme';
import { BottomTabInset, MaxContentWidth, Spacing } from '@/lib/theme';
import { getLibraryPage } from '@/services/library';
import type { LibraryMediaType, LibraryPage } from '@/types/library';

export function LibraryList({ type, title }: { type: LibraryMediaType; title: string }) {
  const theme = useTheme();
  const [result, setResult] = useState<LibraryPage | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const version = useRef(0);

  const load = useCallback(async (page = 1) => {
    const request = ++version.current;
    setLoading(true);
    setError(null);
    try {
      const data = await getLibraryPage(type, page);
      if (request === version.current) {
        setResult(previous => page === 1 || !previous ? data : {
          ...data,
          items: [...previous.items, ...data.items.filter(item =>
            !previous.items.some(existing => existing.externalId === item.externalId))],
        });
      }
    } catch (problem) {
      if (request === version.current) setError(problem instanceof Error ? problem.message : 'Could not load this list.');
    } finally {
      if (request === version.current) setLoading(false);
    }
  }, [type]);

  useFocusEffect(useCallback(() => {
    void load();
    return () => { version.current += 1; };
  }, [load]));

  return (
    <>
      <Stack.Screen options={{ title }} />
      <FlatList
        data={result?.items}
        keyExtractor={item => item.externalId}
        style={styles.list}
        contentContainerStyle={styles.content}
        ListHeaderComponent={
          <View style={styles.header}>
            {Platform.OS === 'web' && (
              <View style={styles.webHeader}>
                <Link href="/my-library" asChild>
                  <Pressable accessibilityRole="link">
                    <Text style={[styles.backText, { color: theme.textSecondary, textDecorationLine: 'underline' }]}>Back to My Library</Text>
                  </Pressable>
                </Link>
                <Text accessibilityRole="header" style={[styles.title, { color: theme.text }]}>{title}</Text>
              </View>
            )}
            {error && <Text accessibilityRole="alert" style={{ color: theme.textSecondary }}>{error}</Text>}
          </View>
        }
        renderItem={({ item }) => (
          <Link href={{ pathname: '/my-library/[type]/[id]', params: { type, id: item.externalId } }} asChild>
            <Pressable accessibilityLabel={`Open details for ${item.title}`}
              style={({ pressed }) => pressed && styles.pressed}>
              <Movie movie={item} />
            </Pressable>
          </Link>
        )}
        ListEmptyComponent={loading ? <ActivityIndicator accessibilityLabel="Loading list" color={theme.text} />
          : !error ? <Text style={{ color: theme.textSecondary }}>Nothing saved in this list yet.</Text> : undefined}
        ListFooterComponent={result?.hasMore ? (
          <Button variant="secondary" size="lg" disabled={loading} onPress={() => void load(result.page + 1)}>
            <Text style={{ color: theme.text }}>Load more</Text>
          </Button>
        ) : undefined}
      />
    </>
  );
}

const styles = StyleSheet.create({
  list: { flex: 1, width: '100%', maxWidth: MaxContentWidth, alignSelf: 'center' },
  content: {
    gap: Spacing.three,
    paddingHorizontal: Spacing.four,
    paddingTop: Platform.OS === 'web' ? WEB_TAB_BAR_OFFSET + Spacing.three : Spacing.three,
    paddingBottom: BottomTabInset + Spacing.four,
  },
  header: { gap: Spacing.two },
  webHeader: { gap: Spacing.two },
  backText: { fontSize: 14 },
  title: { fontSize: 28, fontWeight: '700' },
  pressed: { opacity: 0.7 },
});
