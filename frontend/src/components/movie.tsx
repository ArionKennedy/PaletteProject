import { Image } from 'expo-image';
import { StyleSheet, Text, View } from 'react-native';

import { useTheme } from '@/hooks/use-theme';
import type { MediaSearchResult } from '@/types/mediaSearchResult';

type MovieProps = {
  movie: MediaSearchResult;
};

export default function Movie({ movie }: MovieProps) {
  const theme = useTheme();

  return (
    <View style={[styles.container, { backgroundColor: theme.backgroundElement }]}>
      {movie.posterUrl ? (
        <Image
          source={{ uri: movie.posterUrl }}
          style={styles.poster}
          contentFit="cover"
          accessibilityLabel={`${movie.title} poster`}
        />
      ) : (
        <View style={[styles.poster, styles.posterPlaceholder]}>
          <Text style={[styles.placeholderText, { color: theme.textSecondary }]}>
            No poster
          </Text>
        </View>
      )}
      <View style={styles.details}>
        <Text style={[styles.title, { color: theme.text }]}>{movie.title}</Text>
        <Text style={[styles.mediaType, { color: theme.textSecondary }]}>
          {movie.mediaType}
        </Text>
        <Text style={[styles.releaseDate, { color: theme.textSecondary }]}>
          {movie.releaseDate || 'Release date unknown'}
        </Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    gap: 16,
    padding: 12,
    borderRadius: 8,
  },
  poster: {
    width: 72,
    height: 108,
    borderRadius: 4,
  },
  posterPlaceholder: {
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: '#3a3a3a',
  },
  placeholderText: {
    fontSize: 12,
  },
  details: {
    flex: 1,
    gap: 8,
  },
  title: {
    fontSize: 16,
    fontWeight: '600',
  },
  mediaType: {
    fontSize: 14,
    textTransform: 'capitalize',
  },
  releaseDate: {
    fontSize: 14,
  },
});
