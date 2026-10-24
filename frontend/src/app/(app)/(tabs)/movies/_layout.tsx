import { Stack } from 'expo-router';
import { useTheme } from '@/hooks/use-theme';

export default function MoviesLayout() {

    const theme = useTheme();

  return (
    <Stack
        screenOptions={{
            headerStyle: {backgroundColor: theme.background},
            headerTintColor: theme.text,
            headerShadowVisible: false,
            contentStyle: {backgroundColor: theme.background},
            headerBackButtonDisplayMode: 'minimal'
        }}>
      <Stack.Screen name="index" options={{ headerShown: false }} />
      <Stack.Screen name="[id]" options={{ title: '' }} />
    </Stack>
  );
}
