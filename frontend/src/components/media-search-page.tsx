import { useRef, useState } from 'react';
import {
  ActivityIndicator,
  FlatList,
  Platform,
  Pressable,
  StyleSheet,
  Text,
  View,
  type ListRenderItem,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Link } from 'expo-router';
import { Href } from 'expo-router'


import Movie from '@/components/movie';
import SearchBar from '@/components/searchBar';
import { useTheme } from '@/hooks/use-theme';
import { BottomTabInset, MaxContentWidth, Spacing } from '@/lib/theme';
import { mediaSearch } from '@/services/search';
import type { MediaSearchResult } from '@/types/mediaSearchResult';
import { PageHeader } from '@/components/page-header';
import { recordSearchSelection } from '@/services/recommendation-interactions';

type MediaSearchProps = {
    mediaType: string;
    title: string;
    getDetailHref: (id: string) => Href;
};

export default function MediaSearchPage({mediaType, title, getDetailHref} : MediaSearchProps) {
  const theme = useTheme();
  const [searchResults, setSearchResults] = useState<MediaSearchResult[]>([]);
  const [hasSearched, setHasSearched] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Holds the request that is currently running, so a new search can cancel it.
  const abortRef = useRef<AbortController | null>(null);

  async function searchMedia(query: string) {
    abortRef.current?.abort();

    const controller = new AbortController();
    abortRef.current = controller;

    setIsLoading(true);
    setError(null);

    try {
      const results = await mediaSearch(mediaType, query, controller.signal);
      setSearchResults(results);           // set even when empty, so old results are cleared
      setHasSearched(true);
    } catch (e) {
      if ((e as Error).name === 'AbortError') return;   // replaced by a newer search
      setError((e as Error).message);
      setSearchResults([]);
    } finally {
      if (abortRef.current === controller) setIsLoading(false);
    }
  }

  async function handleSearchSelection(item: MediaSearchResult) {
    try {
      await recordSearchSelection(item.mediaType, item.externalId);
    } catch (error) {
      console.warn('Failed to record search selection:', error);
    }
  }
  
  const renderItem: ListRenderItem<MediaSearchResult> = ({ item }) => (
    <Link href={getDetailHref(item.externalId)} asChild>
      <Pressable
        onPress={() => void handleSearchSelection(item)}
        accessibilityRole="link"
        accessibilityLabel={`Open details for ${item.title}`}
        style={({ pressed }) => pressed && styles.pressed}
      >
        <Movie movie={item} />
      </Pressable>
    </Link>
  );

  // Shown when the list is empty: loading, an error, "no results", or nothing yet.
  function renderEmpty() {
    if (isLoading) {
      return <ActivityIndicator color={theme.text} style={styles.message} />;
    }
    if (error) {
      return <Text style={[styles.message, { color: theme.textSecondary }]}>{error}</Text>;
    }
    if (hasSearched) {
      return <Text style={[styles.message, { color: theme.textSecondary }]}>No results found.</Text>;
    }
    return null;
  }

  return (
    <SafeAreaView style={[styles.safeArea, { backgroundColor: theme.background }]}>
      {Platform.OS !== 'web' ? (
        <PageHeader title={title} />
      ) : null}
      <View style={[styles.searchBar, Platform.OS === 'web' && { marginTop: 80 }]}>
        <SearchBar onSubmitSearch={searchMedia} />
      </View>
      <FlatList
        data={searchResults}
        renderItem={renderItem}
        keyExtractor={(item) => item.externalId}
        ListEmptyComponent={renderEmpty}
        keyboardShouldPersistTaps="handled"
        showsVerticalScrollIndicator={false}
        contentContainerStyle={[
          styles.listContent,
          { paddingBottom: Platform.OS === 'web' ? Spacing.three : BottomTabInset + Spacing.three },
        ]}
        style={styles.list}
      />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    width: '100%',
    alignSelf: 'center',
    maxWidth: MaxContentWidth,
  },
  searchBar: {
    alignItems: 'center',
    paddingHorizontal: Spacing.four,
    paddingVertical: Spacing.three,
  },
  logoutButton: {
    alignSelf: 'flex-start',
    marginLeft: Spacing.four,
  },
  list: {
    flex: 1,
  },
  listContent: {
    gap: Spacing.three,
    paddingHorizontal: Spacing.four,
    paddingTop: Spacing.three,
  },
  message: {
    marginTop: Spacing.five,
    textAlign: 'center',
  },
  pressed: {
    opacity: 0.7,
  },
});
