import * as React from 'react';
import { FlatList, Platform, StyleSheet } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { PageHeader } from '@/components/page-header';
import { useTheme } from '@/hooks/use-theme';
import { BottomTabInset, MaxContentWidth, Spacing } from '@/lib/theme';

export default function Explore() {
  const theme = useTheme();
  return (
    <SafeAreaView style={[styles.safeArea, { backgroundColor: theme.background}]}>
      {Platform.OS !== 'web' ? (
        <PageHeader title="Explore" />
      ) : null}
      <FlatList data={[]} renderItem={() => null} ListHeaderComponent={undefined} ListFooterComponent={undefined} style={styles.list} />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  list: {
    flex: 1,
  },
  safeArea: {
    flex: 1,
    width: '100%',
    alignSelf: 'center',
    gap: Spacing.three,
    paddingBottom: BottomTabInset + Spacing.three,
    maxWidth: MaxContentWidth,
  },
});
