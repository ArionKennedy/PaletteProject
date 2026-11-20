import {
  Tabs,
  TabList,
  TabTrigger,
  TabSlot,
  TabTriggerSlotProps,
  TabListProps,
} from 'expo-router/ui';
import { LibraryBig, type LucideIcon } from 'lucide-react-native';
import { Pressable, Text, View, StyleSheet } from 'react-native';

import { useTheme } from '@/hooks/use-theme';

import { MaxContentWidth, Spacing } from '@/lib/theme';
import { UserMenu } from '@/components/user-menu';

export default function AppTabs() {
  return (
    <Tabs>
      <TabSlot style={{ flex: 1 }} />
      <TabList asChild>
        <CustomTabList>
          <TabTrigger name="movies" href="/movies" asChild>
            <TabButton>Movies</TabButton>
          </TabTrigger>
          <TabTrigger name="tv" href="/tv" asChild>
            <TabButton>Tv Shows</TabButton>
          </TabTrigger>
          <TabTrigger name="books" href="/books" asChild>
            <TabButton>Books</TabButton>
          </TabTrigger>
          <TabTrigger name="games" href="/games" asChild>
            <TabButton>Games</TabButton>
          </TabTrigger>
          <TabTrigger name="my-library" href="/my-library" asChild>
            <TabButton icon={LibraryBig}>My Library</TabButton>
          </TabTrigger>
        </CustomTabList>
      </TabList>
    </Tabs>
  );
}

// Use the native element ref rather than Expo Router's legacy View component ref.
type TabButtonProps = Omit<TabTriggerSlotProps, 'ref'> &
  React.RefAttributes<React.ComponentRef<typeof Pressable>> & { icon?: LucideIcon };

export function TabButton({ children, isFocused, icon: Icon, ...props }: TabButtonProps) {
  const theme = useTheme();

  return (
    <Pressable {...props} style={({ pressed }) => pressed && styles.pressed}>
      <View
        style={[
          styles.tabButtonView,
          { backgroundColor: theme[isFocused ? 'backgroundSelected' : 'backgroundElement'] },
        ]}>
        {Icon && <Icon size={16} color={theme[isFocused ? 'text' : 'textSecondary']} />}
        <Text
          style={[styles.tabLabel, { color: theme[isFocused ? 'text' : 'textSecondary'] }]}>
          {children}
        </Text>
      </View>
    </Pressable>
  );
}

export function CustomTabList(props: TabListProps) {
  const theme = useTheme();

  return (
    <View {...props} style={styles.tabListContainer}>
      <View style={[styles.innerContainer, { backgroundColor: theme.backgroundElement }]}>
        <Text style={[styles.brandText, { color: theme.text }]}>Palette</Text>

        {props.children}
        <UserMenu />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  tabListContainer: {
    position: 'absolute',
    top: 0,
    left: 0,
    width: '100%',
    zIndex: 1,
    padding: Spacing.three,
    justifyContent: 'center',
    alignItems: 'center',
    flexDirection: 'row',
  },
  innerContainer: {
    paddingVertical: Spacing.two,
    paddingHorizontal: Spacing.five,
    borderRadius: Spacing.five,
    flexDirection: 'row',
    alignItems: 'center',
    flexGrow: 1,
    gap: Spacing.two,
    maxWidth: MaxContentWidth,
  },
  brandText: {
    marginRight: 'auto',
    fontSize: 14,
    lineHeight: 20,
    fontWeight: 700,
  },
  tabLabel: {
    fontSize: 14,
    lineHeight: 20,
    fontWeight: 500,
  },
  pressed: {
    opacity: 0.7,
  },
  tabButtonView: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.two,
    paddingVertical: Spacing.one,
    paddingHorizontal: Spacing.three,
    borderRadius: Spacing.three,
  },
  externalPressable: {
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    gap: Spacing.one,
    marginLeft: Spacing.three,
  },
});
