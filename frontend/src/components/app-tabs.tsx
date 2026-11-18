import { NativeTabs } from 'expo-router/unstable-native-tabs';

import { useTheme } from '@/hooks/use-theme';


export default function AppTabs() {
  const theme = useTheme();
  return (
    <NativeTabs
      backgroundColor={theme.background}
      indicatorColor={theme.backgroundElement}
      labelStyle={{ selected: { color: theme.text } }}>
      <NativeTabs.Trigger name="movies">
        <NativeTabs.Trigger.Label>Movies</NativeTabs.Trigger.Label>
        <NativeTabs.Trigger.Icon
          sf="film" md="movie"
        />
      </NativeTabs.Trigger>

      <NativeTabs.Trigger name='tv'>
        <NativeTabs.Trigger.Label>Tv Shows</NativeTabs.Trigger.Label>
        <NativeTabs.Trigger.Icon 
          sf="tv" md="tv"
        />
      </NativeTabs.Trigger>

      <NativeTabs.Trigger name="books">
        <NativeTabs.Trigger.Label>Books</NativeTabs.Trigger.Label>
        <NativeTabs.Trigger.Icon
          sf="book" md="menu_book"
        />
      </NativeTabs.Trigger>


      <NativeTabs.Trigger name="explore">
        <NativeTabs.Trigger.Label>Explore</NativeTabs.Trigger.Label>
        <NativeTabs.Trigger.Icon
          src={require('@/assets/images/tabIcons/explore.png')}
          renderingMode="template"
        />
      </NativeTabs.Trigger>
      <NativeTabs.Trigger name="my-shows">
        <NativeTabs.Trigger.Label>My Shows</NativeTabs.Trigger.Label>
      </NativeTabs.Trigger>
    </NativeTabs>
  );
}
