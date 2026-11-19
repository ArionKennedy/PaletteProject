import { Image } from 'expo-image';
import { Star } from 'lucide-react-native';
import { StyleSheet, View } from 'react-native';

import { Text } from '@/components/ui/text';
import { Spacing } from '@/lib/theme';
import { useTheme } from '@/hooks/use-theme';
import type { ShowLog } from '@/types/show-log';

type ReviewProps = {
  item: ShowLog;
};

export function Review({ item }: ReviewProps) {
  const theme = useTheme();

  return (
    <View style={[styles.row, { backgroundColor: theme.backgroundElement }]}>
      {item.posterUrl ? (
        <Image
          source={{ uri: item.posterUrl }}
          style={styles.poster}
          contentFit="cover"
          accessibilityLabel={`${item.title} poster`}
        />
      ) : (
        <View style={[styles.poster, styles.posterPlaceholder, { backgroundColor: theme.backgroundSelected }]}>
          <Text variant="small" style={{ color: theme.textSecondary }}>No poster</Text>
        </View>
      )}
      <View style={styles.details}>
        <View style={styles.metadata}>
          <Text variant="h4" style={[{ color: theme.text }]}>
            {item.title}
          </Text>
          <Text variant="small" style={[{ color: theme.textSecondary }, styles.mediaType]}>
            {item.mediaType === 'TVShow' ? 'TV Show' : item.mediaType}
          </Text>
          <Text variant="small" style={{ color: theme.textSecondary }}>
            {item.releaseDate ? `Released · ${item.releaseDate}` : 'Release date unknown'}
          </Text>
          <Text variant="small" style={{ color: theme.textSecondary }}>
            Reviewed · {item.lastReviewedAt.slice(0, 10)}
          </Text>
        </View>

        <View style={[styles.review, { borderLeftColor: theme.backgroundSelected }]}>
          <View
            accessibilityLabel={`${item.rating} out of 5 stars`}
            style={styles.rating}
          >
            <Text
              variant="small"
              style={[{ color: theme.textSecondary }, styles.ratingLabel]}
            >
              Review
            </Text>
            {Array.from({ length: 5 }, (_, index) => (
              <Star
                key={index}
                size={16}
                color="#F5C518"
                fill={index < item.rating ? '#F5C518' : 'transparent'}
              />
            ))}
          </View>
          <Text style={{ color: theme.textSecondary }} numberOfLines={5}>
            {item.body || 'No written review'}
          </Text>
        </View>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.three,
    padding: Spacing.three,
    borderRadius: Spacing.two,
    marginBottom: Spacing.two,
  },
  poster: { width: 72, height: 108, borderRadius: 4 },
  posterPlaceholder: { alignItems: 'center', justifyContent: 'center', padding: Spacing.two },
  details: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'flex-start',
    alignSelf: 'flex-start',
    gap: Spacing.one,
  },
  metadata: { flex: 0.8, minWidth: 0, gap: 6 },
  mediaType: { marginBottom: 6 },
  rating: { flexDirection: 'row', alignItems: 'center', gap: 1 },
  ratingLabel: { marginRight: Spacing.two },
  review: {
    flex: 1.2,
    minWidth: 0,
    alignSelf: 'stretch',
    borderLeftWidth: 2,
    paddingLeft: Spacing.two,
    gap: Spacing.half,
  },
});
