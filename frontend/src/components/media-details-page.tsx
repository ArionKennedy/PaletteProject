import { ActivityIndicator, Platform, ScrollView, StyleSheet, Text, View } from 'react-native';
import { Image } from 'expo-image';
import { Stack } from 'expo-router';

import { POSTER_ASPECT_RATIO, POSTER_WIDTH, WEB_TAB_BAR_OFFSET } from '@/constants/moviesConstants';
import { useTheme } from '@/hooks/use-theme';
import { MaxContentWidth, Spacing } from '@/lib/theme';
import type { MediaDetail } from '@/types/mediaDetail'


type MediaDetailsProps = {
    metaText: string;
    errorTitle: string;  // "Movie" or "TV Show"   
    media: MediaDetail | null;        
    error: string | null;                     
};

export default function MediaDetailsPage({metaText, errorTitle, media, error} : MediaDetailsProps) {
  const theme = useTheme();


  if (error) {
    return (
      <View style={[styles.centered, { backgroundColor: theme.background }]}>
        <Stack.Screen options={{ title: errorTitle }} />
        <Text style={{ color: theme.textSecondary }}>{error}</Text>
      </View>
    );
  }

  if (!media) {
    return (
      <View style={[styles.centered, { backgroundColor: theme.background }]}>
        <ActivityIndicator color={theme.text} />
      </View>
    );
  }

  return (
    <ScrollView
      style={{ backgroundColor: theme.background }}
      showsVerticalScrollIndicator={false}
      contentContainerStyle={[
        styles.content,
        Platform.OS === 'web' && { paddingTop: WEB_TAB_BAR_OFFSET },
      ]}>
      <Stack.Screen options={{ title: media.title }} />

      {media.posterUrl ? (
        <Image
          source={{ uri: media.posterUrl }}
          style={styles.poster}
          contentFit="cover"
          accessibilityLabel={`${media.title} poster`}
        />
      ) : (
        <View style={[styles.poster, styles.posterPlaceholder, { backgroundColor: theme.backgroundElement }]}>
          <Text style={{ color: theme.textSecondary }}>No poster</Text>
        </View>
      )}

      <Text style={[styles.title, { color: theme.text }]}>{media.title}</Text>

      <Text style={[styles.meta, { color: theme.textSecondary }]}>
        {metaText}
      </Text>

      <Text style={[styles.description, { color: theme.text }]}>
        {media.description || 'No description available.'}
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
