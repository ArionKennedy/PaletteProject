
import { useCallback, useState } from 'react';
import {
  ActivityIndicator,
  FlatList,
  Platform,
  Pressable,
  StyleSheet,
  Text,
  View,
} from 'react-native';

import { Link, useFocusEffect } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';

import Movie from '@/components/movie';
import { PageHeader } from '@/components/page-header';
import { useTheme } from '@/hooks/use-theme';
import { getMovieRecommendations } from '@/services/recommendations';
import type { MediaSearchResult } from '@/types/mediaSearchResult';

import {
  BottomTabInset,
  MaxContentWidth,
  Spacing,
} from '@/lib/theme';

export default function RecommendationList() {
  const theme = useTheme();

  const [movies, setMovies] = useState<MediaSearchResult[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadRecommendations = useCallback(async () => {
    try {
      setError(null);

      const results = await getMovieRecommendations(20);

      console.log('[Explore] Recommendations:', results);
      console.log('[Explore] Count:', results.length);

      setMovies(results);
    } catch (err) {
      console.error('[Explore] Request failed:', err);

      setError(
        err instanceof Error
          ? err.message
          : 'Unable to load recommendations.',
      );
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      setLoading(true);
      void loadRecommendations();
    }, [loadRecommendations]),
  );

  const refresh = () => {
    setRefreshing(true);
    void loadRecommendations();
  };

  const renderEmpty = () => {
    if (loading) {
      return (
        <ActivityIndicator
          color={theme.text}
          size="large"
          style={styles.message}
        />
      );
    }

    if (error) {
      return (
        <View style={styles.emptyContainer}>
          <Text
            style={[
              styles.message,
              { color: theme.textSecondary },
            ]}
          >
            {error}
          </Text>
        </View>
      );
    }

    return (
      <View style={styles.emptyContainer}>
        <Text
          style={[
            styles.message,
            { color: theme.textSecondary },
          ]}
        >
          No recommendations yet. Save or review some
          movies to build your preferences.
        </Text>
      </View>
    );
  };

  return (
    <SafeAreaView
      style={[
        styles.container,
        { backgroundColor: theme.background },
      ]}
    >
      <View style={styles.contentWrapper}>
        {Platform.OS !== 'web' && (
          <PageHeader title="Explore" />
        )}

        <FlatList
          style={styles.list}
          data={movies}
          keyExtractor={(item) => item.externalId}
          refreshing={refreshing}
          onRefresh={refresh}
          showsVerticalScrollIndicator={false}
          contentContainerStyle={styles.listContent}
          ListHeaderComponent={
            <View style={styles.header}>
              <Text
                style={[
                  styles.heading,
                  { color: theme.text },
                ]}
              >
                Recommended for You
              </Text>

              <Text
                style={[
                  styles.subtitle,
                  { color: theme.textSecondary },
                ]}
              >
                Discover movies based on your interests.
              </Text>
            </View>
          }
          renderItem={({ item }) => (
            <Link
              href={{
                pathname: '/movies/[id]',
                params: {
                  id: item.externalId,
                },
              }}
              asChild
            >
              <Pressable
                accessibilityRole="link"
                accessibilityLabel={`View ${item.title}`}
                style={({ pressed }) =>
                  pressed && styles.pressed
                }
              >
                <Movie movie={item} />
              </Pressable>
            </Link>
          )}
          ListEmptyComponent={renderEmpty}
        />
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    width: '100%',
  },

  contentWrapper: {
    flex: 1,
    width: '100%',
    maxWidth: MaxContentWidth,
    alignSelf: 'center',
  },

  list: {
    flex: 1,
  },

  listContent: {
    paddingHorizontal: Spacing.four,
    paddingTop:
      Platform.OS === 'web'
        ? 90
        : Spacing.three,
    paddingBottom:
      Platform.OS === 'web'
        ? Spacing.three
        : BottomTabInset + Spacing.three,
    gap: Spacing.three,
    flexGrow: 1,
  },

  header: {
    gap: Spacing.two,
    marginBottom: Spacing.three,
  },

  heading: {
    fontSize: 24,
    fontWeight: '700',
  },

  subtitle: {
    fontSize: 14,
  },

  emptyContainer: {
    paddingVertical: Spacing.five,
    alignItems: 'center',
    justifyContent: 'center',
  },

  message: {
    marginTop: Spacing.three,
    textAlign: 'center',
    fontSize: 14,
  },

  pressed: {
    opacity: 0.7,
  },
});
