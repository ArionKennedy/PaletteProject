import { useEffect, useState } from 'react';
import { ActivityIndicator, Platform, ScrollView, StyleSheet, Text, View } from 'react-native';
import { Image } from 'expo-image';
import { Stack, useLocalSearchParams } from 'expo-router';

import { POSTER_ASPECT_RATIO, POSTER_WIDTH, WEB_TAB_BAR_OFFSET } from '@/constants/moviesConstants';
import { useTheme } from '@/hooks/use-theme';
import { MaxContentWidth, Spacing } from '@/lib/theme';
import { movieLookup } from '@/services/lookup';
import type { MovieLookupResult } from '@/types/movieLookupResult';

// 148 -> "2h 28m", 45 -> "45m"
function formatRuntime(minutes: number): string {
  const hours = Math.floor(minutes / 60);
  const rest = minutes % 60;
  return hours > 0 ? `${hours}h ${rest}m` : `${rest}m`;
}

export default function MovieDetailsPage() {
  const theme = useTheme();
  const { id } = useLocalSearchParams<{ id: string }>();   // "27205" from /movies/27205
  const [movie, setMovie] = useState<MovieLookupResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    setMovie(null);
    setError(null);

    movieLookup(id, controller.signal)
      .then(setMovie)
      .catch((e: Error) => {
        if (e.name !== 'AbortError') setError(e.message);
      });

    // Cancel the request if the user leaves before it finishes.
    return () => controller.abort();
  }, [id]);

  if (error) {
    return (
      <View style={[styles.centered, { backgroundColor: theme.background }]}>
        <Stack.Screen options={{ title: 'Movie' }} />
        <Text style={{ color: theme.textSecondary }}>{error}</Text>
      </View>
    );
  }

  if (!movie) {
    return (
      <View style={[styles.centered, { backgroundColor: theme.background }]}>
        <ActivityIndicator color={theme.text} />
      </View>
    );
  }

  const releaseYear = movie.releaseDate?.slice(0, 4);   // "2010-07-15" -> "2010"

  return (
    <ScrollView
      style={{ backgroundColor: theme.background }}
      showsVerticalScrollIndicator={false}
      contentContainerStyle={[
        styles.content,
        Platform.OS === 'web' && { paddingTop: WEB_TAB_BAR_OFFSET },
      ]}>
      <Stack.Screen options={{ title: movie.title }} />

      {movie.posterUrl ? (
        <Image
          source={{ uri: movie.posterUrl }}
          style={styles.poster}
          contentFit="cover"
          accessibilityLabel={`${movie.title} poster`}
        />
      ) : (
        <View style={[styles.poster, styles.posterPlaceholder, { backgroundColor: theme.backgroundElement }]}>
          <Text style={{ color: theme.textSecondary }}>No poster</Text>
        </View>
      )}

      <Text style={[styles.title, { color: theme.text }]}>{movie.title}</Text>

      <Text style={[styles.meta, { color: theme.textSecondary }]}>
        {[releaseYear ?? 'Release date unknown', movie.runtime != null && formatRuntime(movie.runtime)]
          .filter(Boolean)
          .join(' · ')}
      </Text>

      <Text style={[styles.description, { color: theme.text }]}>
        {movie.description || 'No description available.'}
      </Text>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: Spacing.four,
  },
  content: {
    width: '100%',
    maxWidth: MaxContentWidth,
    alignSelf: 'center',
    alignItems: 'center',
    gap: Spacing.three,
    padding: Spacing.four,
  },
  poster: {
    width: POSTER_WIDTH,
    aspectRatio: POSTER_ASPECT_RATIO,
    borderRadius: Spacing.two,
  },
  posterPlaceholder: {
    alignItems: 'center',
    justifyContent: 'center',
  },
  title: {
    fontSize: 24,
    fontWeight: '700',
    textAlign: 'center',
  },
  meta: {
    fontSize: 14,
  },
  description: {
    fontSize: 16,
    lineHeight: 24,
    alignSelf: 'stretch',
  },
});
