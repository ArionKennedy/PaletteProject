import { useEffect, useState } from 'react';
import { ActivityIndicator, Platform, ScrollView, StyleSheet, Text, View } from 'react-native';
import { Image } from 'expo-image';
import { Stack, useLocalSearchParams } from 'expo-router';

import { POSTER_ASPECT_RATIO, POSTER_WIDTH, WEB_TAB_BAR_OFFSET } from '@/constants/moviesConstants';
import { useTheme } from '@/hooks/use-theme';
import { MaxContentWidth, Spacing } from '@/lib/theme';
import { BookLookupResult } from '@/types/bookLookupResult';
import { bookLookup } from '@/services/lookup';


export default function BookDetailsPage() {
  const theme = useTheme();
  const { id } = useLocalSearchParams<{ id: string }>();   // "27205" from /movies/27205
  const [book, setBook] = useState<BookLookupResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    setBook(null);
    setError(null);

    bookLookup(id, controller.signal)
      .then(setBook)
      .catch((e: Error) => {
        if (e.name !== 'AbortError') setError(e.message);
      });

    // Cancel the request if the user leaves before it finishes.
    return () => controller.abort();
  }, [id]);

  if (error) {
    return (
      <View style={[styles.centered, { backgroundColor: theme.background }]}>
        <Stack.Screen options={{ title: 'Book' }} />
        <Text style={{ color: theme.textSecondary }}>{error}</Text>
      </View>
    );
  }

  if (!book) {
    return (
      <View style={[styles.centered, { backgroundColor: theme.background }]}>
        <ActivityIndicator color={theme.text} />
      </View>
    );
  }

  const releaseYear = book.releaseDate?.slice(0, 4);
  const metaText = [
    releaseYear ? `First Air: ${releaseYear}` : null
  ]
    .filter(Boolean)
    .join(' · ');

  return (
    <ScrollView
      style={{ backgroundColor: theme.background }}
      showsVerticalScrollIndicator={false}
      contentContainerStyle={[
        styles.content,
        Platform.OS === 'web' && { paddingTop: WEB_TAB_BAR_OFFSET },
      ]}>
      <Stack.Screen options={{ title: book.title}} />

      {book.posterUrl ? (
        <Image
          source={{ uri: book.posterUrl }}
          style={styles.poster}
          contentFit="cover"
          accessibilityLabel={`${book.title} poster`}
        />
      ) : (
        <View style={[styles.poster, styles.posterPlaceholder, { backgroundColor: theme.backgroundElement }]}>
          <Text style={{ color: theme.textSecondary }}>No poster</Text>
        </View>
      )}

      <Text style={[styles.title, { color: theme.text }]}>{book.title}</Text>

      <Text style={[styles.meta, { color: theme.textSecondary }]}>
        {metaText ? (
          <Text style={[styles.meta, { color: theme.textSecondary }]}>
            {metaText}
          </Text>
        ) : null}

      </Text>

      <Text style={[styles.description, { color: theme.text }]}>
        {book.description || 'No description available.'}
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
