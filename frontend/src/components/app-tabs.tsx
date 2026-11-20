
import { NativeTabs } from 'expo-router/unstable-native-tabs';

import { MEDIA_TAB_ICONS } from '@/constants/media-tab-icons';
import { useTheme } from '@/hooks/use-theme';

export default function AppTabs() {
  const theme = useTheme();

  return (
    <NativeTabs
      backgroundColor={theme.background}
      indicatorColor={theme.backgroundElement}
      labelStyle={{
        selected: { color: theme.text },
      }}
    >
      <NativeTabs.Trigger name="explore">
        <NativeTabs.Trigger.Label>
          Explore
        </NativeTabs.Trigger.Label>
        <NativeTabs.Trigger.Icon
          sf="safari"
          md="explore"
        />
      </NativeTabs.Trigger>

      <NativeTabs.Trigger name="movies">
        <NativeTabs.Trigger.Label>
          Movies
        </NativeTabs.Trigger.Label>
        <NativeTabs.Trigger.Icon
          {...MEDIA_TAB_ICONS.movie}
        />
      </NativeTabs.Trigger>

      <NativeTabs.Trigger name="tv">
        <NativeTabs.Trigger.Label>
          Tv Shows
        </NativeTabs.Trigger.Label>
        <NativeTabs.Trigger.Icon
          {...MEDIA_TAB_ICONS.tv}
        />
      </NativeTabs.Trigger>

      <NativeTabs.Trigger name="books">
        <NativeTabs.Trigger.Label>
          Books
        </NativeTabs.Trigger.Label>
        <NativeTabs.Trigger.Icon
          {...MEDIA_TAB_ICONS.book}
        />
      </NativeTabs.Trigger>

      <NativeTabs.Trigger name="my-library">
        <NativeTabs.Trigger.Label>
          My Library
        </NativeTabs.Trigger.Label>
        <NativeTabs.Trigger.Icon
          sf="square.stack"
          md="collections_bookmark"
        />
      </NativeTabs.Trigger>
    </NativeTabs>
  );
}