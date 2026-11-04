import { ActivityIndicator, FlatList, Platform, Pressable, StyleSheet, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Text } from '@/components/ui/text';
import { BottomTabInset, MaxContentWidth, Spacing } from '@/lib/theme';
import { useTheme } from '@/hooks/use-theme';
import type { ShowLog } from '@/types/show-log';

type ShowListProps = {
  shows: ShowLog[];
  loading: boolean;
  error: string | null;
  hasMore: boolean;
  onRefresh: () => void;
  onLoadMore: () => void;
};

export function ShowList({
  shows, loading, error, hasMore, onRefresh, onLoadMore,
}: ShowListProps) {
  const theme = useTheme();
  return (
    <SafeAreaView style={[styles.screen, { backgroundColor: theme.background }]}>
      <FlatList
        style={styles.list}
        contentContainerStyle={styles.content}
        data={shows}
        keyExtractor={(show) => show.mediaId}
        ListHeaderComponent={
          <View style={styles.header}>
            <Text variant="h3" style={{ color: theme.text }}>My Shows</Text>
            <Text style={{ color: theme.textSecondary }}>Titles you have reviewed.</Text>
            <Pressable accessibilityRole="button" disabled={loading} onPress={onRefresh}
              style={[styles.button, { backgroundColor: theme.backgroundSelected }]}>
              <Text style={{ color: theme.text }}>{loading ? 'Loading…' : 'Refresh shows'}</Text>
            </Pressable>
            {error && <Text style={{ color: theme.text }} accessibilityRole="alert">{error}</Text>}
            <Text variant="h4" style={{ color: theme.text }}>Reviewed titles</Text>
          </View>
        }
        renderItem={({ item }) => (
          <View style={[styles.row, { backgroundColor: theme.backgroundElement }]}>
            <Text style={{ color: theme.text }}>{item.title}</Text>
            <Text variant="small" style={{ color: theme.textSecondary }}>
              Reviewed · {item.lastReviewedAt.slice(0, 10)}
            </Text>
          </View>
        )}
        ListEmptyComponent={
          loading ? <ActivityIndicator accessibilityLabel="Loading shows" color={theme.text} />
            : !error ? <Text style={{ color: theme.text }}>You haven&apos;t reviewed any titles yet.</Text> : undefined
        }
        ListFooterComponent={hasMore ? (
          <Pressable accessibilityRole="button" disabled={loading} onPress={onLoadMore}
            style={[styles.button, { backgroundColor: theme.backgroundSelected }]}>
            <Text style={{ color: theme.text }}>{loading ? 'Loading…' : 'Load more shows'}</Text>
          </Pressable>
        ) : undefined}
      />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1 },
  list: { flex: 1, width: '100%', maxWidth: MaxContentWidth, alignSelf: 'center' },
  content: {
    paddingHorizontal: Spacing.four,
    paddingTop: Platform.OS === 'web' ? Spacing.six + Spacing.five : Spacing.three,
    paddingBottom: BottomTabInset + Spacing.four,
  },
  header: { gap: Spacing.three, marginBottom: Spacing.three },
  row: { padding: Spacing.three, borderRadius: Spacing.two, marginBottom: Spacing.two },
  button: { padding: Spacing.three, borderRadius: Spacing.two, alignItems: 'center' },
});
