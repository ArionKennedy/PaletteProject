import { useCallback, useRef, useState } from 'react';
import { ActivityIndicator, Platform, Pressable, ScrollView, StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Link, useFocusEffect } from 'expo-router';
import { Image } from 'expo-image';
import { SymbolView } from 'expo-symbols';
import { useAuth } from '@clerk/expo';
import { ChevronRight, RefreshCw } from 'lucide-react-native';
import Animated, { Easing, ReduceMotion, useAnimatedStyle, useSharedValue, withTiming } from 'react-native-reanimated';

import { PageHeader } from '@/components/page-header';
import { MEDIA_TAB_ICONS } from '@/constants/media-tab-icons';
import { POSTER_ASPECT_RATIO, WEB_TAB_BAR_OFFSET } from '@/constants/moviesConstants';
import { useTheme } from '@/hooks/use-theme';
import { BottomTabInset, MaxContentWidth, Spacing } from '@/lib/theme';
import { getLibraryPreviews } from '@/services/library';
import { LIBRARY_CATEGORIES, type LibraryPreviews } from '@/types/library';
import type { MediaSearchResult } from '@/types/mediaSearchResult';

export default function LibraryHome() {
  const { sessionId } = useAuth();
  return <LibraryOverview key={sessionId} />;
}

function LibraryOverview() {
  const theme = useTheme();
  const [previews, setPreviews] = useState<LibraryPreviews | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const version = useRef(0);
  const refreshRotation = useSharedValue(0);
  const refreshIconStyle = useAnimatedStyle(() => ({
    transform: [{ translateY: 1 }, { rotate: `${refreshRotation.get()}deg` }],
  }));

  const refresh = useCallback(async () => {
    const request = ++version.current;
    setLoading(true);
    setError(null);
    try {
      const result = await getLibraryPreviews();
      if (request === version.current) setPreviews(result);
    } catch (problem) {
      if (request === version.current) setError(problem instanceof Error ? problem.message : 'Could not load your library.');
    } finally {
      if (request === version.current) setLoading(false);
    }
  }, []);

  useFocusEffect(useCallback(() => {
    void refresh();
    return () => { version.current += 1; };
  }, [refresh]));

  function refreshFromButton() {
    refreshRotation.set(0);
    refreshRotation.set(withTiming(360, {
      duration: 450,
      easing: Easing.out(Easing.cubic),
      reduceMotion: ReduceMotion.System,
    }));
    void refresh();
  }

  return (
    <SafeAreaView style={[styles.screen, { backgroundColor: theme.background }]}>
      <ScrollView contentContainerStyle={styles.content}>
        <PageHeader title="My Library" showUserMenu={Platform.OS !== 'web'} titleAction={
          <Pressable
            accessibilityRole="button"
            accessibilityLabel="Refresh My Library"
            accessibilityState={{ busy: loading, disabled: loading }}
            disabled={loading}
            hitSlop={{ top: 11, bottom: 11 }}
            onPress={refreshFromButton}
            style={({ pressed }) => [styles.iconButton, (pressed || loading) && styles.pressed]}>
            <Animated.View style={refreshIconStyle}>
              <RefreshCw size={22} strokeWidth={2.6} color={theme.text} />
            </Animated.View>
          </Pressable>
        } />
        {error && <Text accessibilityRole="alert" style={[styles.message, { color: theme.textSecondary }]}>{error}</Text>}
        {loading && !previews && <ActivityIndicator accessibilityLabel="Loading library" color={theme.text} />}
        {LIBRARY_CATEGORIES.map(({ type, label }) => {
          const items = previews?.[type];
          const icon = MEDIA_TAB_ICONS[type];
          return (
            <View key={type} style={[styles.shelf, { backgroundColor: theme.backgroundElement }]}>
              <Link href={{ pathname: '/my-library/[type]', params: { type } }} asChild>
                <TouchableOpacity
                  accessibilityRole="link"
                  accessibilityLabel={`See all saved ${label}`}
                  activeOpacity={0.65}
                  style={styles.shelfHeading}>
                  <SymbolView name={{ ios: icon.sf, android: icon.md, web: icon.md }}
                    size={23} tintColor={theme.text} />
                  <Text style={[styles.label, { color: theme.text }]}>{label}</Text>
                  <ChevronRight size={22} color={theme.text} />
                </TouchableOpacity>
              </Link>
              {items?.length === 0 && (
                <Text style={[styles.message, { color: theme.textSecondary }]}>Nothing saved yet.</Text>
              )}
              {items && items.length > 0 && (
                <ScrollView
                  horizontal
                  showsHorizontalScrollIndicator={Platform.OS === 'web'}
                  contentContainerStyle={styles.posters}>
                  {items.map(item => (
                    <Link
                      key={item.externalId}
                      href={{ pathname: '/my-library/[type]/[id]', params: { type, id: item.externalId } }}
                      asChild>
                      <TouchableOpacity
                        accessibilityRole="link"
                        accessibilityLabel={`Open details for ${item.title}`}
                        activeOpacity={0.65}
                        style={styles.poster}>
                        <LibraryCover item={item} />
                      </TouchableOpacity>
                    </Link>
                  ))}
                </ScrollView>
              )}
            </View>
          );
        })}
      </ScrollView>
    </SafeAreaView>
  );
}

function LibraryCover({ item }: { item: MediaSearchResult }) {
  const theme = useTheme();
  const [failedUrl, setFailedUrl] = useState<string | null>(null);
  const url = item.posterUrl;
  return (
    <View style={[styles.cover, { backgroundColor: theme.backgroundSelected }]}>
      {url && url !== failedUrl ? (
        <Image source={{ uri: url }} style={StyleSheet.absoluteFill}
          contentFit="cover" onError={() => setFailedUrl(url)} />
      ) : (
        <Text numberOfLines={4} style={[styles.placeholder, { color: theme.textSecondary }]}>{item.title}</Text>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1 },
  content: {
    width: '100%',
    maxWidth: MaxContentWidth,
    alignSelf: 'center',
    paddingTop: Platform.OS === 'web' ? WEB_TAB_BAR_OFFSET + Spacing.three : Spacing.two,
    paddingBottom: BottomTabInset + Spacing.four,
    gap: Spacing.three,
  },
  iconButton: { width: 44, height: 22, alignItems: 'center', justifyContent: 'center' },
  pressed: { opacity: 0.55 },
  shelf: {
    marginHorizontal: Spacing.three,
    paddingTop: Spacing.one,
    paddingBottom: Spacing.three,
    borderRadius: 12,
    overflow: 'hidden',
  },
  shelfHeading: {
    minHeight: 48,
    paddingHorizontal: Spacing.three,
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.two,
  },
  label: { flex: 1, fontSize: 20, fontWeight: '700' },
  posters: { gap: Spacing.two, paddingHorizontal: Spacing.three },
  poster: { width: 88, height: 88 / POSTER_ASPECT_RATIO, borderRadius: Spacing.two, overflow: 'hidden' },
  cover: { flex: 1, alignItems: 'center', justifyContent: 'center' },
  placeholder: { paddingHorizontal: Spacing.two, textAlign: 'center' },
  message: { marginHorizontal: Spacing.three },
});
