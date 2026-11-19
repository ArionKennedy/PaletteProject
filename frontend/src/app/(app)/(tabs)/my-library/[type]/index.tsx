import { Link, Stack, useLocalSearchParams } from 'expo-router';
import { Platform, StyleSheet, Text, View } from 'react-native';
import { useAuth } from '@clerk/expo';

import { LibraryList } from '@/components/library-list';
import { WEB_TAB_BAR_OFFSET } from '@/constants/moviesConstants';
import { useTheme } from '@/hooks/use-theme';
import { Spacing } from '@/lib/theme';
import { LIBRARY_CATEGORIES } from '@/types/library';

export default function LibraryCategoryScreen() {
  const { type } = useLocalSearchParams<{ type: string }>();
  const { sessionId } = useAuth();
  const category = LIBRARY_CATEGORIES.find(item => item.type === type);
  const theme = useTheme();

  if (!category) {
    return (
      <View style={styles.message}>
        <Stack.Screen options={{ title: 'My Library' }} />
        <Text style={{ color: theme.text }}>This list is unavailable.</Text>
        <Link href="/my-library" style={{ color: theme.text }}>Back to My Library</Link>
      </View>
    );
  }

  return <LibraryList key={`${sessionId}:${category.type}`} type={category.type} title={category.label} />;
}

const styles = StyleSheet.create({
  message: { padding: Spacing.four, paddingTop: Platform.OS === 'web' ? WEB_TAB_BAR_OFFSET : Spacing.four, gap: Spacing.two },
});
