import { Stack } from 'expo-router';
import { Platform } from 'react-native';

import { useTheme } from '@/hooks/use-theme';

export default function LibraryStackLayout() {
  const theme = useTheme();
  return (
    <Stack screenOptions={{
      headerShown: Platform.OS !== 'web',
      headerStyle: { backgroundColor: theme.background },
      headerTintColor: theme.text,
      headerShadowVisible: false,
      contentStyle: { backgroundColor: theme.background },
      headerBackButtonDisplayMode: 'minimal',
    }}>
      <Stack.Screen name="index" options={{ headerShown: false }} />
      <Stack.Screen name="[type]/index" options={{ title: '' }} />
      <Stack.Screen name="[type]/[id]" options={{ title: '' }} />
    </Stack>
  );
}
