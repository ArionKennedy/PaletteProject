import { ActivityIndicator, FlatList, Platform, Pressable, StyleSheet, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Text } from '@/components/ui/text';
import { Review } from '@/components/review';
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
        showsVerticalScrollIndicator={false}
        data={shows}
        keyExtractor={(show) => show.mediaId}
        ListHeaderComponent={
          <View style={styles.header}>
            <Text variant="h3" style={{ color: theme.text }}>My Shows</Text>
            <Text style={{ color: theme.textSecondary }}>Titles you have reviewed.</Text>
            {error && <Text style={{ color: theme.text }} accessibilityRole="alert">{error}</Text>}
            <View style={styles.sectionHeading}>
              <Text variant="h4" style={{ color: theme.text }}>Reviewed titles</Text>
              <Pressable accessibilityRole="button" disabled={loading} onPress={onRefresh}
                style={styles.refreshButton}>
                <Text style={{ color: theme.text, textDecorationLine: 'underline' }}>Refresh shows</Text>
              </Pressable>
            </View>
          </View>
        }
        renderItem={({ item }) => <Review item={item} />}
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
  sectionHeading: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: Spacing.two },
  refreshButton: { flexShrink: 0 },
  button: { padding: Spacing.three, borderRadius: Spacing.two, alignItems: 'center' },
});
