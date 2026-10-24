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

import Movie from '@/components/movie';
import SearchBar from '@/components/searchBar';
import { useTheme } from '@/hooks/use-theme';
import { BottomTabInset, MaxContentWidth, Spacing } from '@/lib/theme';
import { mediaSearch } from '@/services/search';
import type { MediaSearchResult } from '@/types/mediaSearchResult';
import { useAuth } from '@clerk/expo';
import { Button } from '@/components/ui/button';

export default function MoviesSearchPage() {
  const theme = useTheme();
  const [searchResults, setSearchResults] = useState<MediaSearchResult[]>([]);
  const [hasSearched, setHasSearched] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { signOut } = useAuth();

  // Holds the request that is currently running, so a new search can cancel it.
  const abortRef = useRef<AbortController | null>(null);

  async function searchMovie(query: string) {
    abortRef.current?.abort();

    const controller = new AbortController();
    abortRef.current = controller;

    setIsLoading(true);
    setError(null);

    try {
      const results = await mediaSearch("movie", query, controller.signal);
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

  const renderMovieItem: ListRenderItem<MediaSearchResult> = ({ item }) => (
    <Link href={{ pathname: '/movies/[id]', params: { id: item.externalId } }} asChild>
      <Pressable
        accessibilityRole="link"
        accessibilityLabel={`Open details for ${item.title}`}
        style={({ pressed }) => pressed && styles.pressed}>
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
      return <Text style={[styles.message, { color: theme.textSecondary }]}>No movies found.</Text>;
    }
    return null;
  }

  return (
    <SafeAreaView style={[styles.safeArea, { backgroundColor: theme.background }]}>
      <View style={[styles.searchBar, Platform.OS === 'web' && { marginTop: 80 }]}>
        <SearchBar onSubmitSearch={searchMovie} />
      </View>
      <Button onPress={() => signOut()} style={styles.logoutButton}>
        <Text>Logout</Text>
      </Button>
      <FlatList
        data={searchResults}
        renderItem={renderMovieItem}
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
