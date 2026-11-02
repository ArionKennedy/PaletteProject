import { useEffect, useState } from 'react';
import { ActivityIndicator, Platform, ScrollView, StyleSheet, Text, View } from 'react-native';
import { Image } from 'expo-image';
import { Stack, useLocalSearchParams } from 'expo-router';

import { POSTER_ASPECT_RATIO, POSTER_WIDTH, WEB_TAB_BAR_OFFSET } from '@/constants/moviesConstants';
import { useTheme } from '@/hooks/use-theme';
import { MaxContentWidth, Spacing } from '@/lib/theme';
import { TvLookupResult } from '@/types/tvLookupResult';
import { tvLookup } from '@/services/lookup';


export default function TvDetailsPage() {
  const theme = useTheme();
  const { id } = useLocalSearchParams<{ id: string }>();   // "27205" from /movies/27205
  const [tv, setTv] = useState<TvLookupResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    setTv(null);
    setError(null);

    tvLookup(id, controller.signal)
      .then(setTv)
      .catch((e: Error) => {
        if (e.name !== 'AbortError') setError(e.message);
      });

    // Cancel the request if the user leaves before it finishes.
    return () => controller.abort();
  }, [id]);

  if (error) {
    return (
      <View style={[styles.centered, { backgroundColor: theme.background }]}>
        <Stack.Screen options={{ title: 'Tv' }} />
        <Text style={{ color: theme.textSecondary }}>{error}</Text>
      </View>
    );
  }

  if (!tv) {
    return (
      <View style={[styles.centered, { backgroundColor: theme.background }]}>
        <ActivityIndicator color={theme.text} />
      </View>
    );
  }

  const releaseYear = tv.releaseDate?.slice(0, 4); 
  const seasons = (tv.numberOfSeasons) ? tv.numberOfSeasons + " Seasons" : "1 season"  // "2010-07-15" -> "2010"

  return (
    <ScrollView
      style={{ backgroundColor: theme.background }}
      showsVerticalScrollIndicator={false}
      contentContainerStyle={[
        styles.content,
        Platform.OS === 'web' && { paddingTop: WEB_TAB_BAR_OFFSET },
      ]}>
      <Stack.Screen options={{ title: tv.title}} />

      {tv.posterUrl ? (
        <Image
          source={{ uri: tv.posterUrl }}
          style={styles.poster}
          contentFit="cover"
          accessibilityLabel={`${tv.title} poster`}
        />
      ) : (
        <View style={[styles.poster, styles.posterPlaceholder, { backgroundColor: theme.backgroundElement }]}>
          <Text style={{ color: theme.textSecondary }}>No poster</Text>
        </View>
      )}

      <Text style={[styles.title, { color: theme.text }]}>{tv.title}</Text>

      <Text style={[styles.meta, { color: theme.textSecondary }]}>
        {/* {[releaseYear ?? 'First Air date unknown', tv.numberOfSeasons ?? '1']
          .filter(Boolean)
          .join(' · ')} */}
        {"First Air: " + releaseYear + " · " + seasons}
      </Text>

      <Text style={[styles.description, { color: theme.text }]}>
        {tv.description || 'No description available.'}
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
